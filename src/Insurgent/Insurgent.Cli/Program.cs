﻿using System;
using System.Threading;
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
                pm.Kill();
                pm.Start();
                
                using (var am = new AgentManager(count))
                {
                    am.Kill();

                    Thread.Sleep(1000);

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