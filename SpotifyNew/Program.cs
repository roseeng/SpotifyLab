namespace SpotifyNew
{
    using Microsoft.Extensions.Configuration;
    using SpotifyAPI.Web;
    using SpotifyAPI.Web.Auth;
        
    internal class Program
    {
        private static EmbedIOAuthServer _server;
        private static bool _running = true;
        private static string _clientId;
        private static string _clientSecret;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Next-gen Spotify Playlist Downloader");

            IConfigurationRoot secretConfig = new ConfigurationBuilder().AddJsonFile("Secrets\\secrets.json").Build();
            var section = secretConfig.GetRequiredSection("spotify");

            _clientId = section["clientId"] ?? "MISSING";
            _clientSecret = section["clienSecret"] ?? "MISSING";

            // ---

            // Make sure "http://localhost:5543/callback" is in your spotify application as redirect uri!
            _server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail }
            };
            BrowserUtil.Open(request.ToUri());

            while (_running)
            {
                Thread.Sleep(500);
            }
        }

        private async Task MoreMain(SpotifyClient spotify)
        {
            var lists = await spotify.Playlists.CurrentUsers();
            var otto = lists.Items?.Count;
            var allan = await spotify.PaginateAll(lists);

            var a = 1;
            foreach (var pl in allan)
            {
                Console.WriteLine($"{pl.Name} ({pl.Type})");
            }

            _running = false;
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(
              new AuthorizationCodeTokenRequest(_clientId, _clientSecret, response.Code, new Uri("http://localhost:5543/callback")
              )
            );

            var spotify = new SpotifyClient(tokenResponse.AccessToken);
            // do calls with Spotify and save token?
            var p = new Program();
            await p.MoreMain(spotify);
        }

        private static async Task OnErrorReceived(object sender, string error, string? state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
            _running = false;
        }
    }
}
