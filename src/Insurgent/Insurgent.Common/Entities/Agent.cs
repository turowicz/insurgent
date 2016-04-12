using System;
using System.Diagnostics;

namespace Insurgent.Common.Entities
{
    public class Agent
    {
        public Int32 Id { get; set; }

        public Process Process { get; set; }

        public Agent(Int32 id, Process process)
        {
            Id = id;
            Process = process;

            Process.OutputDataReceived += Process_OutputDataReceived;
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
