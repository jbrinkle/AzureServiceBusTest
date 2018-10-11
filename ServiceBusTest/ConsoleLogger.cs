using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ServiceBusTest
{
    internal class ConsoleLogger
    {
        private readonly Thread consoleWriter;
        private readonly Queue<string> messages;
        private readonly TextWriter consoleOut;

        public bool IsActive { get; private set; }

        public ConsoleLogger(TextWriter outstream)
        {
            consoleOut = outstream;
            messages = new Queue<string>();
            consoleWriter = new Thread(ConsoleWriterThread);
        }

        public void Start()
        {
            IsActive = true;
            consoleWriter.Start();
        }

        public void Stop()
        {
            IsActive = false;
            consoleWriter.Join();
        }
        
        public void WriteOutput(string message)
        {
            messages.Enqueue(message);
        }

        private void ConsoleWriterThread()
        {
            while (IsActive || messages.Count > 0)
            {
                while (messages.TryDequeue(out string message))
                {
                    var now = DateTime.Now.ToString("HH:mm:ss.fff");
                    consoleOut.WriteLine($"[{now}] {message}");
                }

                Thread.Sleep(2);
            }
        }
    }
}
