using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASCOM;
using ASCOM.Wise40.Common;
using ASCOM.Wise40;
using System.Net.Http;
using System.Threading;

namespace TestPeriodicHttpFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            PeriodicHttpFetcher[] fetchers =
            {
                new PeriodicHttpFetcher(
                    name: "Shutter range",
                    url: "http://192.168.1.6/range",
                    period: TimeSpan.FromSeconds(10)),

                new PeriodicHttpFetcher(
                    name: "Sun elevation",
                    url: "https://api.ipgeolocation.io/astronomy?" +
                        "apiKey=d6ce0c7ecb5c451ba2b462dfb5750364&" +
                        $"lat={WiseSite.Latitude}&" +
                        $"long={WiseSite.Longitude}",
                    period: TimeSpan.FromMinutes(1)),
            };

            while (true)
            {
                Random r = new Random();
                int i = r.Next(0, fetchers.Length);

                try
                {
                    string value = fetchers[i].Result;
                    Console.WriteLine($"{fetchers[i].Name}: value: {value}, age: {fetchers[i].Age.ToMinimalString()}");
                }
                catch (InvalidValueException)
                {}
                finally
                {
                    Thread.Sleep(5000);
                }
            }
        }
    }
}
