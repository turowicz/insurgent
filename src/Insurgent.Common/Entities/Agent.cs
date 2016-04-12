using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Insurgent.Common.Entities
{
    public class Agent
    {
        public Int32 Id { get; set; }

        public Process Process { get; set; }

        public Int32 Progress { get; set; }

        public Agent(Int32 id, Process process)
        {
            Id = id;
            Process = process;
            Progress = 0;

            Task.Run(() => Intercept());
        }

        private void Intercept()
        {
            var lineRx = new Regex(@"(Bootstrapped [0-9]*%)");
            var valueRx = new Regex(@"\d+");

            while (!Process.HasExited || Progress != 100)
            {
                var line = Process.StandardOutput.ReadLine();

                if (!String.IsNullOrWhiteSpace(line))
                {

                    var lineMatches = lineRx.Matches(line);

                    foreach (var lineMatch in lineMatches)
                    {
                        Progress = Convert.ToInt32(valueRx.Match(lineMatch.ToString()).ToString());
                    }
                }
            }
        }
    }
}
