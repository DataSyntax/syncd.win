using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using SyncD.Data.Concrete;
using SyncD.Data.Enumerations;

namespace SyncD.Infrastructure
{
    public class SyncDaemon
    {
        private bool _needSynchronization = false;
        private const int ThreeSeconds = 3000;
        private const string StartMessage = "syncd has started watching current directory";
        private const string StopMessage = "syncd has stopped watching current directory";
        private const string SynchronizationFinishedMessage = "syncd has finished synchronization of files";

        private Timer _synchronizationTimer;
        private FileStream _locker;
        private Settings _settings;
        private readonly ExeRunner _notifier;
        private readonly ExeRunner _synchronizer;

        public SyncDaemon(Settings settings)
        {
            _settings = settings;

            _notifier = new ExeRunner(OnNotificationMessageReceived, OnError);
            _synchronizer = new ExeRunner(OnSynchronizationMessageReceived, OnError);

            StartSynchronizationJob();
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
                ProcessHelper.KillAllProcessesSpawnedBy(process.Id);
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
            int processId;
            var isRunning = IsRunning(out processId);

            Console.WriteLine(isRunning
                                        ? string.Format("syncd is running: PID - {0}", processId)
                                        : "syncd is stopped");
        }

        #endregion

        #region Private methods

        private bool LockFolder()
        {
            try
            {
                var processId = Process.GetCurrentProcess().Id.ToString();
                _locker = new FileStream(_settings.PidFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                _locker.Seek(0, SeekOrigin.Begin);
                _locker.SetLength(processId.Length);

                var streamWriter = new StreamWriter(_locker);
                streamWriter.Write(processId);
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

        private void OnNotificationMessageReceived(string message)
        {
            _needSynchronization = true;

            if (_settings.Verbose == Verbose.Talkative)
            {
                LogMessage(message);
            }
        }

        private void OnError(string message)
        {
            if (_settings.Verbose == Verbose.Talkative)
            {
                LogMessage(message);
            }
        }

        private void OnSynchronizationMessageReceived(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
                if (_settings.Verbose == Verbose.Talkative)
                {
                    LogMessage(message);
                }
            }
        }

        private void StartSynchronizationJob()
        {
            var autoEvent = new AutoResetEvent(false);
            TimerCallback callback = Synchronize;
            _synchronizationTimer = new Timer(callback, autoEvent, 0, ThreeSeconds);
        }

        private void Synchronize(object stateInfo)
        {
            if (_needSynchronization && !_synchronizer.IsRunning)
            {
                _needSynchronization = false;
                _synchronizer.Do(_settings.SyncCommand);
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