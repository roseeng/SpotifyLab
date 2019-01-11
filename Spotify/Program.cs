using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.IO;

using SpotifyAPI;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Spotify
{
    class Program
    {
        /*
          
         Steg ett: Logga in på Spotify
         Steg två: Öppna nedanstående URL i Chrome (skillnaden är bara att jag testar att skicka med ett state i den senare)
         
https://accounts.spotify.com/authorize/?client_id=b80c989bca714f4b9544319ac76c8c33&response_type=token&redirect_uri=http://localhost&state=&scope=playlist-read-private&show_dialog=False

https://accounts.spotify.com/authorize/?client_id=b80c989bca714f4b9544319ac76c8c33&response_type=token&redirect_uri=http://localhost&state=123&scope=playlist-read-private&show_dialog=False
         
         Steg tre: Chrome kommer göra en redirect som inte kan öppnas (om du inte har en webbserver igång på din dator)
                   Urlen kommer se ut ungefär: http://localhost/#access_token=BQDyI2OMfVebN0haQovN58UKyQUHQYRNZL94UNTU0U0DxWqurGSsZIRSYoiuMvnlNKgPoS9gpUaXEbFVdQlYJp4lyQpu_F-wSuhtO4ORa3mWTsNanNm1agqhfTV39MIK0bEaKbZ2OafkOWfBUBz_cCHhjf-qe_2E3ro&token_type=Bearer&expires_in=3600&state=123
         
         Steg fyra: Kopiera access_token ur Urlen och klistra in i konstruktorn för SPotifyWebAPI nedan. Kör programmet.
         
         */
        /*
http://localhost/#
access_token=BQBbdryq3UCMOoD5BgGRzGkQpd_7queFqzt0ifYMkF6RTIpi-jWAhpv35BWTmbsgG_9c-PXwQA_rOsjnVMLbIPmpMijLo1Jg_WtXN-CbbQPPDyytaF6qsmtGjv_D8YiV5rUj8q6pe_Igy9DA_hCAdehiZwegRIh1jcnbSbxNjDY

&token_type=Bearer
&expires_in=3600
&state=         
         */

        //static string _ClientId = "b80c989bca714f4b9544319ac76c8c33";
        //static SpotifyWebAPI _spotify;

        static void Main(string[] args)
        {
            var clientId = "b80c989bca714f4b9544319ac76c8c33";
                       
            var token = OAuth.GetToken(clientId);

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("No token received. Check the chrome window.");
                return;
            }

            var spotify = new SpotifyWebAPI()
            {
                TokenType = "Bearer",
                AccessToken = token, // "BQBPGjGB5WiAn-DvNlP13H9Rsx92neTLpKAIkDjwpdatUfflFrS1VgJmI5-UvXDtmdG7qwcaPcs_cF1sZeZrohegyh274oYbY__S7demnSn4P4SWw_cEgiXgL2E2g8G8vy9Xug4zuwM5FPOx3gTqZCyUXOTh7pjvtVYsGQ",
                UseAuth = true
            };

            var userId = "roseeng";

            var prof = spotify.GetPrivateProfile();
            if (prof.HasError())
            {
                Console.WriteLine("Error logging in to the Spotify API. Message: " + prof.Error.Message);
                return;
            }

            Console.WriteLine("Logged in as: " + prof.DisplayName);
            
            HashSet<SimplePlaylist> Playlists = new HashSet<SimplePlaylist>();
            //HashSet<CompositePlaylist> Playlists = new HashSet<CompositePlaylist>();
            HashSet<FullTrack> Tracks = new HashSet<FullTrack>();
            HashSet<Snapshot> Snapshots = new HashSet<Snapshot>();
            //            MultiMap<string, string> PlaylistTracks = new MultiMap<string, string>();
            HashSet<PlaylistTrack> PlaylistTracks = new HashSet<PlaylistTrack>();

            bool goOn = true;
            for (int offset = 0; goOn; offset += 20) {
                var playlists = spotify.GetUserPlaylists(userId, 20, offset);

                if (playlists.HasError()) {
                    if (playlists.Error.Status == 429) {
                        Console.WriteLine("GetUserPlaylists Offset: " + offset + ", RLE, wait " + playlists.Header("Retry-After") + " seconds");
                        var dummy = playlists.Headers();
                        Thread.Sleep(30 * 1000);
                        playlists = spotify.GetUserPlaylists(userId, 20, offset);
                    } else {
                        Console.WriteLine("GetUserPlaylists err " + playlists.Error.Message);
                        break; ;
                    }
                }

                goOn = playlists.HasNextPage();

                foreach (var playlist in playlists.Items) {
                    Console.WriteLine("  * " + playlist.Name + ", id: " + playlist.Id + ", tracks: " + playlist.Tracks.Total);
                    //var data2 = Newtonsoft.Json.JsonConvert.SerializeObject(playlist);
                    //var p2 = Newtonsoft.Json.JsonConvert.DeserializeObject<CompositePlaylist>(data2);
                    Playlists.Add(playlist);

                    // Fancy workaround for json-graphql-server's automatic Id matching:
                    Snapshot snap = new Snapshot() { Id = playlist.SnapshotId };
                    Snapshots.Add(snap);

                    var tracks = spotify.GetPlaylistTracks(userId, playlist.Id);
                    if (tracks.HasError()) {
                        if (tracks.Error.Status == 429) {
                            Console.WriteLine("GetPlaylistTracks " + playlist.Name + ", RLE, wait " + tracks.Header("Retry-After") + " seconds");
                            Thread.Sleep(20 * 1000);
                            tracks = spotify.GetPlaylistTracks(userId, playlist.Id);
                        } else {
                            Console.WriteLine("GetPlaylistTracks " + playlist.Name + ", err " + tracks.Error.Message);
                            continue;
                        }
                    }

                    //var pt = new PlaylistTrack();
                    //pt.playlist_id = playlist.Id;
                    //p2.FullTracks = new HashSet<FullTrack>();

                    foreach (var track in tracks.Items) {
                        //var data = Newtonsoft.Json.JsonConvert.SerializeObject(track.Track);
                        Tracks.Add(track.Track);

                        var pt = new PlaylistTrack();
                        pt.playlist_id = playlist.Id;
                        pt.track_id = track.Track.Id;

                        PlaylistTracks.Add(pt);
                        //p2.FullTracks.Add( track.Track );
//                        Console.WriteLine("  * * " + track.Track.Name + " - " + track.Track.Album.Name  + " - " + Artists(track.Track.Artists) + " " + track.Track.PreviewUrl);
                    }

                    //Playlists.Add(p2);
                    //PlaylistTracks.Add(pt);

                }
            }

            // Start writing: (tailored for use with https://github.com/marmelab/json-graphql-server)
            StreamWriter sw = new StreamWriter("spotifydata.js");

            sw.WriteLine("module.exports = { ");
            sw.WriteLine(" \"snapshots\": ");
            var data3 = Newtonsoft.Json.JsonConvert.SerializeObject(Snapshots);
            sw.WriteLine(data3);

            sw.WriteLine(",");

            sw.WriteLine(" \"playlists\": ");
            data3 = Newtonsoft.Json.JsonConvert.SerializeObject(Playlists);
            sw.WriteLine(data3);

            sw.WriteLine(",");

            sw.WriteLine(" \"tracks\": ");
            data3 = Newtonsoft.Json.JsonConvert.SerializeObject(Tracks);
            sw.WriteLine(data3);

            sw.WriteLine(",");

            sw.WriteLine(" \"playlist_tracks\": ");
            data3 = Newtonsoft.Json.JsonConvert.SerializeObject(PlaylistTracks);
            sw.WriteLine(data3);

            sw.WriteLine("};");

            sw.Close();
        }
    }

    public class Snapshot
    {
        public string Id;
    }

    public class TrackDTO
    {
        public string track_id;
    }

    public class PlaylistTrack
    {
        public PlaylistTrack()
        {
            //            track_ids = new HashSet<string>();
            Id = Guid.NewGuid().ToString();
        }

        public string Id;
        public string playlist_id;
        public string track_id;
  //      public HashSet<string> track_ids;
    }

    public class CompositePlaylist: SimplePlaylist
    {
        public HashSet<FullTrack> FullTracks;
    }
}
