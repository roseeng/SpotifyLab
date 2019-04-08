# SpotifyLab
Download all your playlist metadata

## Usage;
Go to https://developer.spotify.com/dashboard and register an application. Set redirectURL to "http://localhost".
Update clientId in Program.cs to your clientId.

When you start the application it will open a webpage, where you do the OAuth authorization.
Then it redirects (to http://localhost, remember?) with a querystring that contains your token.

This is then used when we call the API to fetch all tracks of all playlists, pausing whenever 
we gate a rate limit error, and writing the result as json to a file.
