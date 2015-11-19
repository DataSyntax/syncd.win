using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SyncD.Infrastructure
{
    public class ExeRunner
    {
        private bool _isRunning;
        private Process _process;
        private Queue<string> _responseQueue;
        private ManualResetEvent _responseEvent;

        private readonly Action<string> _onErrorOccurred;
        private readonly Action<string> _onMessageReceived;

        public bool IsRunning { get { return _isRunning; } }

        public ExeRunner(Action<string> onMessageReceived, Action<string> onErrorOccurred)
        {
            _onMessageReceived = onMessageReceived;
            _onErrorOccurred = onErrorOccurred;
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

            _process = new Process
            {
                EnableRaisingEvents = true,
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

            _process.Start();

            _process.OutputDataReceived += OutDataReceived;
            _process.ErrorDataReceived += ErrorDataReceived;
            _process.Exited += (sender, args) => { _isRunning = false; };

            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            _isRunning = true;

            return _process.Id;
        }

        public void WaitForExit(int milliseconds = 0)
        {
            if (milliseconds == 0)
            {
                _process.WaitForExit();
            }
            else
            {
                _process.WaitForExit(milliseconds);
            }
        }

        #region Private Methods

        private void OutDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            try
            {
                lock (_responseQueue)
                {
                    if (_onMessageReceived != null)
                    {
                        _onMessageReceived(dataReceivedEventArgs.Data);
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
                    if (_onErrorOccurred != null)
                    {
                        _onErrorOccurred(dataReceivedEventArgs.Data);
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