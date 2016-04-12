using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Insurgent.Common.Entities;

namespace Insurgent.Common.Managers
{
    public class AgentManager : IDisposable
    {
        private readonly string _path;
        private readonly int _count;
        private readonly ConcurrentQueue<Agent> _agents;

        public AgentManager(int count)
        {
            _count = count;
            _agents = new ConcurrentQueue<Agent>();
            _path = Environment.GetEnvironmentVariable("INSURGENTTOR");
        }

        public void Start()
        {
            if (_path == null || !Directory.Exists(_path ?? string.Empty))
            {
                throw new ArgumentException("No environment variable INSURGENTTOR");
            }

            var port = 9051;
            var torPath = Path.Combine(_path, "Tor", "tor.exe");

            foreach (var id in Enumerable.Range(1, _count))
            {
                var configPath = Path.Combine(_path, $"{id}.config");
                var dataPath = Path.Combine(_path, "Sessions", id.ToString());

                var command = new StringBuilder();
                command.AppendLine($"SocksPort {port++}");
                command.AppendLine($"DataDirectory {dataPath}");

                if (Directory.Exists(dataPath))
                {
                    Directory.Delete(dataPath, true);
                }

                Directory.CreateDirectory(dataPath);

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                File.WriteAllText(configPath, command.ToString());

                var agent = new Agent(id, Process.Start($"{torPath}", $"--defaults-torrc {configPath}"));

                _agents.Enqueue(agent);
            }
        }

        public void Kill()
        {
            foreach (var process in Process.GetProcessesByName("tor"))
            {
                if (process.ProcessName == "tor")
                {
                    process.Kill();
                }
            }
        }

        public void Stop()
        {
            while (_agents.Any())
            {
                Agent agent;
                _agents.TryDequeue(out agent);

                agent?.Process?.Kill();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public async Task<List<String>> Despatch(Uri url)
        {
            var port = 8118;

            var results = await Task.WhenAll(_agents.Select((agent, i) => Get(port + i, agent, url)));

            return results.ToList();
        }

        private async Task<string> Get(int port, Agent agent, Uri url)
        {
            using (var http = new HttpClient(new HttpClientHandler() { Proxy = new WebProxy($"127.0.0.1:{port}"), UseProxy = true }))
            {
                return await http.GetStringAsync(url);
            }
        }
    }
}
