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
        static string _ClientId = "b80c989bca714f4b9544319ac76c8c33";
        static SpotifyWebAPI _spotify;
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
        static void Main(string[] args)
        {

            StreamWriter sw = new StreamWriter("spotifydata.json");

            var spotify = new SpotifyWebAPI()
            {                
                TokenType = "Bearer",
                AccessToken = "BQDyI2OMfVebN0haQovN58UKyQUHQYRNZL94UNTU0U0DxWqurGSsZIRSYoiuMvnlNKgPoS9gpUaXEbFVdQlYJp4lyQpu_F-wSuhtO4ORa3mWTsNanNm1agqhfTV39MIK0bEaKbZ2OafkOWfBUBz_cCHhjf-qe_2E3ro",
                UseAuth = true
            };

            var userId = "roseeng";

            var prof = spotify.GetPrivateProfile();
            if (prof.HasError())
                Console.WriteLine("err" + prof.Error.Message);

//            Console.WriteLine("Logged in as: " + prof.DisplayName);
            sw.WriteLine("{ \"userId\": \"" + prof.Id + "\",");
            sw.WriteLine("  \"username\": \"" + prof.DisplayName + "\",");
            sw.WriteLine(" \"batches\": [");

            bool goOn = true;
            for (int offset = 0; goOn; offset += 20) {
                var playlists = spotify.GetUserPlaylists(userId, 20, offset);

                if (playlists.HasError()) {
                    if (playlists.Error.Status == 429) {
                        Console.WriteLine("GetUserPlaylists Offset: " + offset + ", RLE, wait " + playlists.Header("Retry-After") + " seconds");
                        Thread.Sleep(30 * 1000);
                        playlists = spotify.GetUserPlaylists(userId, 20, offset);
                    } else {
                        Console.WriteLine("GetUserPlaylists err " + playlists.Error.Message);
                        break; ;
                    }
                }

                goOn = playlists.HasNextPage();

                //Console.WriteLine("OK");
                //Console.WriteLine(playlists.Items.Count + " items");
                sw.WriteLine(" { \"type\": \"batch\", ");
                sw.WriteLine("   \"offset\": " + playlists.Offset + ",");
                sw.WriteLine("   \"count\": " + playlists.Items.Count + ",");
                sw.WriteLine("   \"lists\": [");

                foreach (var playlist in playlists.Items) {
                    //Console.WriteLine("  * " + playlist.Name + ", id: " + playlist.Id + ", tracks: " + playlist.Tracks.Total);
                    var data2 = Newtonsoft.Json.JsonConvert.SerializeObject(playlist);
                    sw.WriteLine("{ \"type\": \"listcontainer\", ");
                    sw.WriteLine("  \"list\": " + data2 + ",");

                    var tracks = spotify.GetPlaylistTracks(userId, playlist.Id);
                    if (tracks.HasError()) {
                        if (tracks.Error.Status == 429) {
                            Console.WriteLine("GetPlaylistTracks " + playlist.Name + ", RLE, wait " + tracks.Header("Retry-After") + " seconds");
                            Thread.Sleep(30 * 1000);
                            tracks = spotify.GetPlaylistTracks(userId, playlist.Id);
                        } else {
                            Console.WriteLine("GetPlaylistTracks " + playlist.Name + ", err " + tracks.Error.Message);
                            sw.WriteLine(" \"tracks\": null }, ");
                            continue;
                        }
                    }

                    sw.WriteLine("\"tracks\": [");
                    foreach (var track in tracks.Items) {
                        var data = Newtonsoft.Json.JsonConvert.SerializeObject(track.Track);
                        sw.WriteLine(data + ",");
//                        Console.WriteLine("  * * " + track.Track.Name + " - " + track.Track.Album.Name  + " - " + Artists(track.Track.Artists) + " " + track.Track.PreviewUrl);
                    }
                    sw.WriteLine("{ \"type\":\"track\" } ]  ");
                    sw.WriteLine(" }, ");
                }
                sw.WriteLine(" { \"type\": \"listcontainer\" } ]");
                sw.WriteLine(" },");
            }
            sw.WriteLine("{ \"type\": \"batch\"} ]");
            sw.WriteLine("}");

            sw.Close();
        }

        private static string Artists(List<SimpleArtist> artists)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var artist in artists)
                sb.Append(artist.Name + ", ");
            var s = sb.ToString();
            if (s.Length > 2)
                s = s.Substring(0, s.Length - 2);
            return s;
        }

        /*
        static ImplicitGrantAuth auth;
        static void Main(string[] args)
        {
            //Create the auth object
            auth = new ImplicitGrantAuth()
            {
                //Your client Id
                ClientId = _ClientId,
                //Set this to localhost if you want to use the built-in HTTP Server
                RedirectUri = "http://localhost",
                //How many permissions we need?
                Scope = Scope.PlaylistReadPrivate,
            };
            //Start the internal http server
            auth.StartHttpServer();
            //When we got our response
            auth.OnResponseReceivedEvent += auth_OnResponseReceivedEvent;
            //Start
            auth.DoAuth();
        }

        static void auth_OnResponseReceivedEvent(Token token, string state) //, string error)
        {
            //stop the http server
            auth.StopHttpServer();

            var spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken
            };

            var r = spotify.GetUserPlaylists("me");
            
            if (r.HasError())
                Console.WriteLine("err" + r.Error.Message);

            Console.WriteLine("OK");
            Console.WriteLine(r.Items.Count + " items");
            //We can now make calls with the token object
        }
        */
        /*
        static void Main(string[] args)
        {
            var p = new Program();
            p.Run(args);
        }

        private void Run(string[] args)
        {
            DoAuth();

            //var profile = api.GetPrivateProfile();
            //if (profile.HasError()) {
            //    var err = profile.Error;
            //} else {
            //    var userId = profile.Id;

            //    var lists = api.GetUserPlaylists(userId);
            //    var x = lists.HasError();
            //    var y = lists.Items;
            //    var z = y.Count;
            //}
        }
         */
        /*
        private ClientCredentialsAuth auth;

        private SpotifyWebAPI DoAuth()
        {
            //Create the auth object
            auth = new ClientCredentialsAuth()
            {
                //Your client Id
                ClientId = "b80c989bca714f4b9544319ac76c8c33",
                //Your client secret UNSECURE!!
                ClientSecret = "72b70cc9e25d4e588e6d38a9934141fe",
                //How many permissions we need?
                Scope = Scope.UserReadPrivate,
            };
            //With this token object, we now can make calls
            Token token = auth.DoAuth();
            var spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken,
                UseAuth = true
            };

            return spotify;
        }
        */
        /*
        static ImplicitGrantAuth a;

        static AutorizationCodeAuth auth;
        static void DoAuth()
        {
            a = new ImplicitGrantAuth();
            a.ClientId = "b80c989bca714f4b9544319ac76c8c33";
            a.Scope = Scope.PlaylistReadPrivate;
            a.RedirectUri = "http://localhost";

            //Create the auth object
            auth = new AutorizationCodeAuth()
            {
                //Your client Id
                ClientId = "b80c989bca714f4b9544319ac76c8c33",
                //Set this to localhost if you want to use the built-in HTTP Server
                RedirectUri = "http://localhost",
                //How many permissions we need?
                Scope = Scope.UserReadPrivate,
            };
            //This will be called, if the user cancled/accept the auth-request
            auth.OnResponseReceivedEvent += auth_OnResponseReceivedEvent;
            //a local HTTP Server will be started (Needed for the response)
            auth.StartHttpServer();
            //This will open the spotify auth-page. The user can decline/accept the request
            auth.DoAuth();

            Thread.Sleep(60000);
            auth.StopHttpServer();
            Console.WriteLine("Too long, didnt respond, exiting now...");
        }

        void dumm7()
        {
            OAuthTokenResponse authorizationTokens = OAuthUtility.GetRequestToken(CONSUMER_KEY, CONSUMER_SECRET, "oob");

            string url = String.Format("http://twitter.com/oauth/authorize?oauth_token={0}", authorizationTokens.Token);
            Console.WriteLine("Go to:\n\n{0}\n\nLogon as @KPCRecipes and enter the pin number below:\n\n", url);
            string pin = Console.ReadLine();

            OAuthTokenResponse accessTokens = OAuthUtility.GetAccessToken(CONSUMER_KEY, CONSUMER_SECRET, authorizationTokens.Token, pin);

            Console.WriteLine("Here are your access tokens:\n\nScreenName: {0}\nToken: {1}\nTokenSecret: {2}\nUserId: {3}\n\n", accessTokens.ScreenName, accessTokens.Token, accessTokens.TokenSecret, accessTokens.UserId.ToString());

        }
        private static void auth_OnResponseReceivedEvent(AutorizationCodeAuthResponse response)
        {
            //Stop the HTTP Server, done.
            auth.StopHttpServer();

            //NEVER DO THIS! You would need to provide the ClientSecret.
            //You would need to do it e.g via a PHP-Script.
            Token token = auth.ExchangeAuthCode(response.Code, "72b70cc9e25d4e588e6d38a9934141fe");

            var api = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken
            };

            //With the token object, you can now make API calls
            var profile = api.GetPrivateProfile();
            if (profile.HasError()) {
                var err = profile.Error;
            } else {
                var userId = profile.Id;

                var lists = api.GetUserPlaylists(userId);
                var x = lists.HasError();
                var y = lists.Items;
                var z = y.Count;
            }
        }
         */ 
    }
         
}
