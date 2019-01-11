using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Spotify
{
    public class OAuth
    {
        public static string GetToken(string username, string password, string code)
        {
            string url = "https://accounts.spotify.com/api/token";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Clear();
                var header =  "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password)));
                httpClient.DefaultRequestHeaders.Add("Authorization", header);
                
                var postdata = new List<KeyValuePair<string, string>>();
                postdata.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
                postdata.Add(new KeyValuePair<string, string>("code", code));
                postdata.Add(new KeyValuePair<string, string>("redirect_uri", "http://localhost"));

                using (var content = new FormUrlEncodedContent(postdata))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    HttpResponseMessage response = httpClient.PostAsync(url, content).Result;

                    if (!response.IsSuccessStatusCode)
                    {
                        var msg = response.Content.ReadAsAsync<ErrDTO>().Result;
                        Console.WriteLine("Error: " + msg.error + " : " + msg.error_description);
                        return null;
                    }
                    var result = response.Content.ReadAsAsync<TokenDTO>().Result;
                    //result = "{\"access_token\":\"BQDTXdhT253kwVABBOPZ5AbVVD5KKQKX0IZPn7Eo1WoOAnx8J9gIbCriY9ozYI8ccZGLEMyZBsYNi6tOlvnmq_LaVEkhGsOpY1Z-bcWBdxl8IbSTa_GIuy8C2g2E6xP_YlFW3b7YhVNHNxTW-VjZ9YWaW-Ciag\",\"token_type\":\"Bearer\",\"expires_in\":3600,\"refresh_token\":\"AQAXsjevVavqTYm1UaH_i1SLIX1hgQ7SigIawO224b94BkfTNDCDs4ojdF5Z7Mf-kBkHvwPM02sDVIDkjpBF2K-ta7VEJW3aAqIXZyDuLlOfOb-klnoGnuz1tyVdYY3xJ-zZCA\",\"scope\":\"playlist-read-private\"}"
                    return result.token;
                }
            }
        }

        public static void Authorize()
        {
            var urlWithParameters = "--remote-debugging-port=9222 " + "https://accounts.spotify.com/authorize/?client_id=b80c989bca714f4b9544319ac76c8c33&response_type=token&redirect_uri=http://localhost&state=123&scope=playlist-read-private&show_dialog=False";
            var proc = System.Diagnostics.Process.Start("chrome.exe", urlWithParameters);

            using (var httpClient = new HttpClient())
            {
                var url = "http://localhost:9222/json";
                HttpResponseMessage response = httpClient.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result; // ReadAsAsync<TokenDTO>().Result;
                }
                else
                {
                    var msg = response.Content.ReadAsStringAsync().Result;
                }
            }
        }

        public static void Authorize2()
        {
            var urlWithParameters = "https://accounts.spotify.com/authorize/?client_id=b80c989bca714f4b9544319ac76c8c33&response_type=token&redirect_uri=http://localhost&state=123&scope=playlist-read-private&show_dialog=False";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlWithParameters);
            request.CookieContainer = new CookieContainer();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var otto = response.GetResponseStream();
            var olle = new StreamReader(otto).ReadToEnd();
        }

        public static string GetToken2(string username, string password, string code)
        {
            string url = "https://accounts.spotify.com/api/token";
            var uri = new Uri(url);

            var webRequest = WebRequest.Create(uri);
            webRequest.Method = "POST";
            webRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(username + ":" + password));

            string data = "&grant_type=authorization_code&code=" + code + "&redirect_uri=http%3A%2F%2Flocalhost";

            var bytes = Encoding.UTF8.GetBytes(data);
            webRequest.ContentLength = bytes.Length;
            webRequest.ContentType = "application/x-www-form-urlencoded";
            using (var requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }

            WebResponse myWebResponse = webRequest.GetResponse();
            var responseStream = myWebResponse.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
            string pageContent = myStreamReader.ReadToEnd();

            responseStream.Close();
            myWebResponse.Close();

            return pageContent;
        }
    }

    public class ErrDTO
    {
        public string error;
        public string error_description;
    }

    public class TokenDTO
    {
        public string token;
        public string scope;
    }
}
