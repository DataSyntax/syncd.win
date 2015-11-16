using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SyncD.Data.Concrete;
using SyncD.Data.Enumerations;
using SyncD.Infrastructure;

namespace SyncD.Daemon
{
    public class SyncDaemon
    {
        private bool _synchronize = false;
        private const string StartMessage = "syncd has started watching current directory";
        private const string StopMessage = "syncd has stopped watching current directory";
        private const string SynchronizationFinishedMessage = "syncd has finished synchronization of files";

        private FileStream _locker;
        private Settings _settings;
        private readonly ExeRunner _notifier;
        private readonly ExeRunner _synchronizer;

        public SyncDaemon(Settings settings)
        {
            _settings = settings;

            _notifier = new ExeRunner(OnNotificationCompleted, OnError);
            _synchronizer = new ExeRunner(OnSynchronizationCompleted, OnError);
        }

        #region Commands

        public bool Start()
        {
            if (!IsRunning())
            {
                if (LockFolder())
                {
                    _notifier.Do(_settings.WatchCommand);
                    Console.WriteLine(StartMessage);
                    LogMessage(StartMessage);

                    return true;
                }

                return false;
            }

            Console.WriteLine("Cannot start syncd since it has already been watching current directory");
            return false;
        }

        public void Stop()
        {
            int processId;

            if (IsRunning(out processId))
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
                process.WaitForExit();
                UnlockFolder();

                Console.WriteLine(StopMessage);
                LogMessage(StopMessage);
            }
            else
            {
                Console.WriteLine("Cannot stop syncd since it is not running");
            }
        }

        public bool Restart(Settings settings)
        {
            _settings = settings;

            if (IsRunning())
            {
                Stop();
            }
            return Start();
        }

        public void Run()
        {
            _synchronizer.Do(_settings.SyncCommand);
            _synchronizer.WaitForExit();
            Console.WriteLine(SynchronizationFinishedMessage);
            LogMessage(SynchronizationFinishedMessage);
        }

        public void Status()
        {
            Console.WriteLine(IsRunning()
                                        ? "syncd is running"
                                        : "syncd is stopped");
        }

        #endregion

        #region Private methods

        private bool LockFolder()
        {
            try
            {
                _locker = new FileStream(_settings.PidFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

                var streamWriter = new StreamWriter(_locker);
                streamWriter.Write(Process.GetCurrentProcess().Id.ToString());
                streamWriter.Flush();

                return true;
            }
            catch (IOException)
            {
                if (_locker != null)
                {
                    _locker.Dispose();
                    _locker.Close();
                }
                Console.WriteLine("Error occurred. Cannot create PID file.");

                return false;
            }
        }

        private void UnlockFolder()
        {
            if (_locker != null)
            {
                _locker.Dispose();
                _locker.Close();
            }

            File.Delete(_settings.PidFileName);
        }

        private bool IsRunning()
        {
            int processId;
            return IsRunning(out processId);
        }

        private bool IsRunning(out int processId)
        {
            processId = 0;

            if (File.Exists(_settings.PidFileName))
            {
                using (var fileStream = new FileStream(_settings.PidFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var streamReader = new StreamReader(fileStream);
                    var pid = streamReader.ReadToEnd();

                    int parsedProcessId;
                    if (int.TryParse(pid, out parsedProcessId))
                    {
                        processId = parsedProcessId;
                        return Process.GetProcessesByName("SyncD.Daemon")
                                      .Any(p => p.Id == parsedProcessId
                                             && Process.GetCurrentProcess().Id != parsedProcessId);
                    }

                    return false;
                }
            }

            return false;
        }

        private void OnNotificationCompleted(string message)
        {
            _synchronize = true;

            if (_settings.Verbose == Verbose.Talkative)
            {
                LogMessage(message);
            }

            //only one synchronization at the moment
            if (!_synchronizer.IsRunning)
            {
                _synchronizer.Do(_settings.SyncCommand);
            }
        }

        private void OnError(string message)
        {
            if (_settings.Verbose == Verbose.Talkative)
            {
                LogMessage(message);
            }
        }

        private void OnSynchronizationCompleted(string message)
        {
            if (_synchronize)
            {
                _synchronize = false;
                if (!_synchronizer.IsRunning)
                {
                    _synchronizer.Do(_settings.SyncCommand);
                }
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
                if (_settings.Verbose == Verbose.Talkative)
                {
                    LogMessage(message);
                }
            }
        }

        private void LogMessage(string message)
        {
            if (_settings.Verbose != Verbose.Silent)
            {
                Log.Instance.WriteToLog(string.Format("{0} ---> {1}", DateTime.Now.ToString("G"), message), _settings.LogFileName);
            }
        }

        #endregion
    }
}