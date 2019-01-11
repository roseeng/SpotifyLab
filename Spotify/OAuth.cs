using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

// Requires reference to WebDriver.Support.dll
using OpenQA.Selenium.Support.UI;

namespace Spotify
{
    public class OAuth
    {
        static string cookiefile = "cookies.txt";

        public static string GetToken(string clientId)
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                // We must be at the right domain to be able to load cookies
                driver.Navigate().GoToUrl("https://www.spotify.com");

                RestoreCookies(driver);

                // Check that we can login
                var overviewUrl = "https://www.spotify.com/se/account/overview/";

                //Notice navigation is slightly different than the Java version
                //This is because 'get' is a keyword in C#
                driver.Navigate().GoToUrl(overviewUrl);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                wait.Until(d => d.Url.ToLower().StartsWith(overviewUrl));

                // Save cookies, since we are logged in now
                PersistCookies(driver);

                // Get a token for our app
                driver.Navigate().GoToUrl("https://accounts.spotify.com/authorize/?client_id=" + clientId + "&response_type=token&redirect_uri=http://localhost&state=123&scope=playlist-read-private&show_dialog=False");

                // Wait for the page to load, timeout after 10 seconds
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                wait.Until(d => d.Url.ToLower().StartsWith("http://localhost")); // This is taken from the redirect_uri parameter above

                var querystring = driver.Url.Substring("http://localhost/#".Length);
                Console.WriteLine("Redirected to: " + driver.Url);
                var parameters = System.Web.HttpUtility.ParseQueryString(querystring);
                return parameters["access_token"];
            }
        }

        static void PersistCookies(IWebDriver driver)
        {
            using (StreamWriter sw = new StreamWriter(cookiefile))
            {
                foreach (var cookie in driver.Manage().Cookies.AllCookies)
                    sw.WriteLine(JsonConvert.SerializeObject(cookie));
            }
        }

        static void RestoreCookies(IWebDriver driver)
        {
            if (!File.Exists(cookiefile))
                return;

            JsonSerializerSettings settings = new JsonSerializerSettings() { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor };

            var count = 0;
            using (StreamReader sr = new StreamReader(cookiefile))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var mycookie = JsonConvert.DeserializeObject<MyCookie>(line, settings);
                    var cookie = mycookie.cookie;

                    driver.Manage().Cookies.AddCookie(cookie);
                    count++;
                }
            }
            Console.WriteLine(count + " cookies restored.");
        }
    }

    public class MyCookie
    {
        public Cookie cookie;

        public MyCookie(string name, string value, string domain, string path, DateTime? expiry)
        {
            cookie = new Cookie(name, value, domain, path, expiry);
        }
    }

}
