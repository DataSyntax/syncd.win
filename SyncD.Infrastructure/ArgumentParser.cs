using System.IO;
using SyncD.Data.Enumerations;
using SyncD.Data.Exceptions;

namespace SyncD.Infrastructure
{
    public class ArgumentParser
    {
        public Arguments Parse(string[] args)
        {
            if (args.Length > 1)
            {
                throw new TooMuchArgsException("Too much arguments have been passed");
            }

            return ParseArgument(args[0]);
        }

        public void PrintUsage(string name, TextWriter writer)
        {
            writer.WriteLine("Usage: " + name + " [option]");
            writer.WriteLine("Options:");
            writer.WriteLine("start:      Start watching all files inside current directory");
            writer.WriteLine("stop:       Stop watching all files inside current directory");
            writer.WriteLine("restart:    Read latest values from config file and restart syncd");
            writer.WriteLine("run:        Synchronize all files inside current directory");
            writer.WriteLine("status:     Check whether current directory is watched");
        }

        #region Private methods

        private Arguments ParseArgument(string option)
        {
            switch (option)
            {
                case "start": { return Arguments.Start; }
                case "stop": { return Arguments.Stop; }
                case "restart": { return Arguments.Restart; }
                case "run": { return Arguments.Run; }
                case "status": { return Arguments.Status; }
                default:
                    {
                        throw new InvalidArgumentException(string.Format("'{0}' is not valid syncd option", option));
                    }
            }
        }

        #endregion
    }
}