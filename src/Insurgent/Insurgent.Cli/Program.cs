using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Insurgent.Common.Entities;
using Insurgent.Common.Managers;

namespace Insurgent.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var count = 30;
            var uri = new Uri("https://api.ipify.org?format=json", UriKind.Absolute);

            using (var pm = new ProxyManager(count))
            {
                using (var am = new AgentManager(count))
                {
                    pm.Kill();
                    am.Kill();

                    pm.Start();
                    am.Start();

                    while (!am.Ready())
                    {
                        Thread.Sleep(100);
                    }

                    Console.WriteLine("Processes started");

                    var task = am.Despatch(async agent =>
                    {
                        var result = await Get(agent, uri);

                        Console.WriteLine(result);
                    });

                    task.Wait(TimeSpan.FromMinutes(5));
                }

                Console.WriteLine("Processes closed");
                Console.ReadLine();
            }
        }

        static async Task<string> Get(Agent agent, Uri url)
        {
            var port = 8118;
            using (var http = new HttpClient(new HttpClientHandler() { Proxy = new WebProxy($"127.0.0.1:{port + agent.Id}"), UseProxy = true }))
            {
                return await http.GetStringAsync(url);
            }
        }
    }
}
