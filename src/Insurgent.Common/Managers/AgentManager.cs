using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Insurgent.Common.Entities;

namespace Insurgent.Common.Managers
{
    public class AgentManager : IDisposable
    {
        private readonly string _path;
        private readonly int _count;
        private readonly string _country;
        private readonly ConcurrentQueue<Agent> _agents;

        public AgentManager(int count, string country = null)
        {
            _count = count;
            _country = country;
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

            foreach (var id in Enumerable.Range(0, _count - 1))
            {
                var configPath = Path.Combine(_path, $"{id}.config");
                var dataPath = Path.Combine(_path, "Sessions", id.ToString());

                var command = new StringBuilder();
                command.AppendLine($"SocksPort {port++}");
                command.AppendLine($"DataDirectory {dataPath}");

                if (!String.IsNullOrWhiteSpace(_country))
                {
                    command.AppendLine($"ExitNodes {{{_country}}}");
                }

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

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = $"{torPath}",
                        Arguments = $"--defaults-torrc {configPath}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                var agent = new Agent(id, process);

                _agents.Enqueue(agent);
            }
        }

        public Boolean Ready()
        {
            return _agents.All(x => x.Progress == 100);
        }

        public void Kill()
        {
            foreach (var process in Process.GetProcessesByName("tor"))
            {
                if (process.ProcessName == "tor")
                {
                    process.Kill();
                    process.WaitForExit();
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
                agent?.Process?.WaitForExit();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public Task Despatch(Func<Agent, Task> action)
        {
            return Task.WhenAll(_agents.Select(action));
        }
    }
}
