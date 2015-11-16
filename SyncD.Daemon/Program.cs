using System;
using System.IO;
using System.Windows.Forms;
using SyncD.Data.Enumerations;
using SyncD.Infrastructure;

namespace SyncD.Daemon
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            //Assume that everything is correct
            var settings = new SettingProvider().Load();
            var arguments = new ArgumentParser().Parse(args);
            var daemon = new SyncDaemon(settings);

            switch (arguments)
            {
                case Arguments.Start:
                    {
                        if (daemon.Start()) { Application.Run(); }
                        break;
                    }
                case Arguments.Stop:
                    {
                        daemon.Stop();
                        break;
                    }
                case Arguments.Restart:
                    {
                        if (daemon.Restart(settings)) { Application.Run(); }
                        break;
                    }
                case Arguments.Run:
                    {
                        daemon.Run();
                        break;
                    }
                case Arguments.Status:
                    {
                        daemon.Status();
                        break;
                    }
            }
        }
    }
}