using System;
using Insurgent.Common.Managers;

namespace Insurgent.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var count = 5;

            using (var pm = new ProxyManager(count))
            {
                pm.Start();
                
                using (var am = new AgentManager(count))
                {
                    am.Start();

                    Console.WriteLine("Processes running");
                    Console.ReadLine();

                    var results = am.Despatch(new Uri("https://api.ipify.org?format=json", UriKind.Absolute)).Result;

                    foreach (var result in results)
                    {
                        Console.WriteLine(result);
                    }
                }

                Console.WriteLine("Processes closed");
                Console.ReadLine();
            }
        }
    }
}
