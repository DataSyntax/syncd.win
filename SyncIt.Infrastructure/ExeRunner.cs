using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SyncIt.Infrastructure
{
    public class ExeRunner
    {
        private Process _bashProcess;
        private Queue<string> _responseQueue;
        private ManualResetEvent _responseEvent;

        private readonly Action<string> _onError;
        private readonly Action<string> _onSuccess;

        public ExeRunner(Action<string> onSuccess, Action<string> onError)
        {
            _onSuccess = onSuccess;
            _onError = onError;
        }

        public int Do(string command)
        {
            _responseQueue = new Queue<string>();
            _responseEvent = new ManualResetEvent(false);

            string fileName;
            var arguments = string.Empty;

            var parts = command.Split(' ');

            if (parts.Length > 1)
            {
                fileName = parts.First();
                arguments = string.Join(" ", parts.Skip(1));
            }
            else
            {
                fileName = command;
            }

            _bashProcess = new Process
            {
                StartInfo = new ProcessStartInfo(fileName)
                {
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    ErrorDialog = false
                }
            };

            _bashProcess.Start();

            var errorEventHandler = new DataReceivedEventHandler(ErrorDataReceived);
            var outEventHandler = new DataReceivedEventHandler(OutDataReceived);
            _bashProcess.OutputDataReceived += outEventHandler;
            _bashProcess.ErrorDataReceived += errorEventHandler;

            _bashProcess.BeginErrorReadLine();
            _bashProcess.BeginOutputReadLine();

            return _bashProcess.Id;
        }

        public void Stop()
        {
            _bashProcess.StandardInput.Close();
            _bashProcess.Close();
        }

        public void WaitForExit(int milliseconds = 0)
        {
            if (milliseconds == 0)
            {
                _bashProcess.WaitForExit();
            }
            else
            {
                _bashProcess.WaitForExit(milliseconds);
            }
        }

        #region Private Methods

        private void OutDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            try
            {
                lock (_responseQueue)
                {
                    if (_onSuccess != null)
                    {
                        _onSuccess(dataReceivedEventArgs.Data);
                    }

                    _responseQueue.Enqueue(dataReceivedEventArgs.Data);
                    _responseEvent.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Data);
            }
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            try
            {
                lock (_responseQueue)
                {
                    if (_onError != null)
                    {
                        _onError(dataReceivedEventArgs.Data);
                    }

                    _responseQueue.Enqueue(dataReceivedEventArgs.Data);
                    _responseEvent.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Data);
            }
        }

        #endregion
    }
}