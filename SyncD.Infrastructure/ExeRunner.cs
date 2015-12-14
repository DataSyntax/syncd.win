using System;
using System.Diagnostics;
using System.Linq;

namespace SyncD.Infrastructure
{
    public class ExeRunner
    {
        private bool _isRunning;
        private Process _process;

        private readonly Action<string> _onErrorOccurred;
        private readonly Action<string> _onMessageReceived;
        private readonly Action _onExit;

        public bool IsRunning { get { return _isRunning; } }

        public ExeRunner(Action<string> onMessageReceived, Action<string> onErrorOccurred)
        {
            _onMessageReceived = onMessageReceived;
            _onErrorOccurred = onErrorOccurred;
        }

        public ExeRunner(Action<string> onMessageReceived, Action<string> onErrorOccurred, Action onExit)
            : this(onMessageReceived, onErrorOccurred)
        {
            _onExit = onExit;
        }

        public Process Do(string command)
        {
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
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                }
            };

            _process.Start();

            _process.OutputDataReceived += (sender, args) =>
            {
                if (_onMessageReceived != null)
                {
                    _onMessageReceived(args.Data);
                }
            };

            _process.ErrorDataReceived += (sender, args) =>
            {
                if (_onErrorOccurred != null)
                {
                    _onErrorOccurred(args.Data);
                }
            };

            _process.Exited += (sender, args) =>
            {
                _isRunning = false;
                if (_onExit != null)
                {
                    _onExit();
                }
            };

            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            _isRunning = true;

            return _process;
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
    }
}