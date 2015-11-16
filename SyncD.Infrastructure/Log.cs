using System.IO;

namespace SyncD.Infrastructure
{
    public class Log
    {
        private static Log _instance;
        private static readonly object Locker = new object();

        private Log()
        { }

        public static Log Instance
        {
            get { return _instance ?? (_instance = new Log()); }
        }

        public void WriteToLog(string message, string filePath)
        {
            lock (Locker)
            {
                var streamWriter = File.AppendText(filePath);
                streamWriter.WriteLine(message);
                streamWriter.Close();
            }
        }
    }
}