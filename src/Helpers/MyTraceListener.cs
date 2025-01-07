using System;
using System.Diagnostics;
using System.IO;

namespace sopra_hris_api.Helpers
{
    public class MyTraceListener : TraceListener
    {
        private InvokerOutputTraceMessage listener;

        public MyTraceListener() { listener = null; }
        public MyTraceListener(InvokerOutputTraceMessage listener) { this.listener = listener; }

        private void OutputToFile(string msg)
        {
            try
            {
                var path =
                    $"{AppDomain.CurrentDomain.SetupInformation.ApplicationBase}traceLogs\\{DateTime.Today:yyyMM}\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                using var writer = new StreamWriter(path + DateTime.Now.ToString("yyyyMMdd") + ".txt", true);
                writer.WriteLine($"[{DateTime.Now:HH:mm:ss.ffff}] {msg}");
                writer.Close();
            }
            catch (Exception) { }
        }

        public override void Write(string msg, string cat)
        {
            WriteLine(string.Format("{0}: {1}", cat, msg));
        }

        public override void Write(string msg)
        {
            WriteLine(msg);
        }

        public override void WriteLine(string msg, string cat)
        {
            WriteLine(string.Format("{0}: {1}", cat, msg));
        }

        public override void WriteLine(string msg)
        {
            OutputToFile(msg);
            Console.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, msg));
            if (listener != null) listener(msg);
        }

        public delegate void InvokerOutputTraceMessage(string message);
    }
}

