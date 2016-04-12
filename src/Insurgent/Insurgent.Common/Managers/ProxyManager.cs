using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Insurgent.Common.Entities;

namespace Insurgent.Common.Managers
{
    public class ProxyManager : IDisposable
    {
        private readonly string _path;
        private readonly int _count;
        private readonly ConcurrentQueue<Proxy> _proxies;

        public ProxyManager(int count)
        {
            _count = count;
            _proxies = new ConcurrentQueue<Proxy>();
            _path = Environment.GetEnvironmentVariable("INSURGENTPRIVOXY");
        }

        public void Start()
        {
            if (_path == null || !Directory.Exists(_path ?? string.Empty))
            {
                throw new ArgumentException("No environment variable INSURGENTPRIVOXY");
            }

            var port = 8118;
            var socks = 9051;
            var proxyPath = Path.Combine(_path, "Privoxy", "privoxy.exe");
            var template = File.ReadAllText(Path.Combine(_path, "config.templ.txt"));

            foreach (var id in Enumerable.Range(0, _count - 1))
            {
                var configPath = Path.Combine(_path, $"config.{id}.txt");
                var dataPath = Path.Combine(_path, "Sessions", id.ToString());

                var config = template.Replace("@@PROXY@@", port++.ToString())
                                     .Replace("@@SOCKS@@", socks++.ToString())
                                     .Replace("@@DIR@@", Path.Combine(_path, "Privoxy"));

                if (Directory.Exists(dataPath))
                {
                    Directory.Delete(dataPath, true);
                }

                Directory.CreateDirectory(dataPath);

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                File.WriteAllText(configPath, config);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = $"{proxyPath}",
                        Arguments = Path.Combine(_path, $"config.{id}.txt"),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var proxy = new Proxy(id, process);

                _proxies.Enqueue(proxy);
            }
        }

        public void Kill()
        {
            foreach (var process in Process.GetProcessesByName("privoxy"))
            {
                if (process.ProcessName == "privoxy")
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        public void Stop()
        {
            while (_proxies.Any())
            {
                Proxy proxy;
                _proxies.TryDequeue(out proxy);

                proxy?.Process?.Kill();
                proxy?.Process?.WaitForExit();
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
