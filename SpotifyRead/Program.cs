using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Newtonsoft.Json;

// 

namespace SpotifyRead
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader("spotifydataOK.json");
            var data = sr.ReadToEnd();

            var dynobj = JsonConvert.DeserializeObject<dynamic>(data);
            Console.WriteLine("Loaded");
        }
    }
}
