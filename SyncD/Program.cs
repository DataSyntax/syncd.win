using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SyncD.Data.Concrete;
using SyncD.Data.Enumerations;
using SyncD.Data.Exceptions;
using SyncD.Infrastructure;

namespace SyncD
{
    class Program
    {
        private const string HelpMessage = "Use -? or --help for getting information about usage of syncd";
        
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
        public delegate bool HandlerRoutine(CtrlTypes ctrlType);

        static void Main(string[] args)
        {
            var settings = LoadAndValidateSettings();
            if (settings == null)
            {
                return;
            }

            var arguments = ParseAndValidateArguments(args);
            if (arguments == Arguments.None)
            {
                return;
            }

            SetConsoleCtrlHandler(ConsoleCtrlCheck, true);

            //This is synchronous command
            if (arguments == Arguments.Status)
            {
                new SyncDaemon(settings).Status();
                return;
            }

            //This is synchronous command
            if (arguments == Arguments.Run)
            {
                new SyncDaemon(settings).Run();
                return;
            }

            var runner = new ExeRunner(Log, Log);
            runner.Do(string.Format("SyncD.Daemon.exe {0}", args[0]));

            if (arguments == Arguments.Start
             || arguments == Arguments.Restart)
            {
                runner.WaitForExit(1000);
            }
            else
            {
                runner.WaitForExit();
            }
        }

        #region Private methods

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            ProcessHelper.KillAllProcessesSpawnedBy(Process.GetCurrentProcess().Id);
            return false;
        }

        private static void Log(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }
        }

        private static Settings LoadAndValidateSettings()
        {
            try
            {
                return new SettingProvider().Load();
            }
            catch (SettingNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private static Arguments ParseAndValidateArguments(string[] args)
        {
            var argumentParser = new ArgumentParser();

            try
            {
                if (args.Length == 0
                 || args[0].Equals("-?")
                 || args[0].Equals("--help"))
                {
                    argumentParser.PrintUsage("syncd", Console.Error);
                    return Arguments.None;
                }

                return argumentParser.Parse(args);
            }
            catch (TooMuchArgsException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(HelpMessage);
                return Arguments.None;
            }
            catch (InvalidArgumentException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(HelpMessage);
                return Arguments.None;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Arguments.None;
            }
        }

        #endregion
    }

    public enum CtrlTypes
    {
        CtrlCEvent = 0,
        CtrlBreakEvent,
        CtrlCloseEvent,
        CtrlLogoffEvent = 5,
        CtrlShutdownEvent
    }
}