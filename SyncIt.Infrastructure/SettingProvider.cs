using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SyncIt.Data.Concrete;
using SyncIt.Data.Enumerations;
using SyncIt.Data.Exceptions;

namespace SyncIt.Infrastructure
{
    public class SettingProvider
    {
        private const string LogFileNameKey = "LOGFILE";
        private const string PidFileNameKey = "PIDFILE";
        private const string WatchCommandKey = "WATCHCOMMAND";
        private const string SyncCommandKey = "SYNCCOMMAND";
        private const string VerboseKey = "VERBOSE";

        public Settings Load()
        {
            if (!File.Exists("syncd.conf"))
            {
                throw new SettingNotFoundException("Config file 'syncd.conf' was not found in target directory");
            }

            var appSettings = File.ReadAllLines("syncd.conf")
                                  .Select(x => x.Split('='))
                                  .Where(x => x.Length > 1)
                                  .ToDictionary(x => x[0].Trim().ToUpper(), x => x[1]);

            var logFileName = GetValueOrThrow(appSettings, LogFileNameKey, "Error occurred. LogFile setting value is missing");
            var pidFileName = GetValueOrThrow(appSettings, PidFileNameKey, "Error occurred. PidFile setting value is missing");
            var watchCommand = GetValueOrThrow(appSettings, WatchCommandKey, "Error occurred. WatchCommand setting value is missing");
            var syncCommand = GetValueOrThrow(appSettings, SyncCommandKey, "Error occurred. SyncCommand setting value is missing");

            Verbose verbose;
            Enum.TryParse(GetValueOrEmpty(appSettings, VerboseKey), true, out verbose);

            var settings = new Settings
            {
                LogFileName = logFileName,
                PidFileName = pidFileName,
                WatchCommand = watchCommand,
                SyncCommand = syncCommand,
                Verbose = verbose
            };

            settings.WatchCommand = string.Format("{0} --exclude {1}|{2}|syncd.conf", settings.WatchCommand, settings.LogFileName, settings.PidFileName);
            settings.SyncCommand = string.Format("{0} --exclude {1} --exclude {2} --exclude syncd.conf", settings.SyncCommand, settings.LogFileName, settings.PidFileName);

            return settings;
        }

        #region Private methods

        private string GetValueOrThrow(Dictionary<string, string> settings, string key, string errorMessage)
        {
            var value = GetValueOrEmpty(settings, key);

            if (string.IsNullOrEmpty(value))
            {
                throw new SettingNotFoundException(errorMessage);
            }

            return value;
        }

        private string GetValueOrEmpty(Dictionary<string, string> settings, string key)
        {
            if (!settings.ContainsKey(key)
              || string.IsNullOrWhiteSpace(settings[key]))
            {
                return string.Empty;
            }

            var value = settings[key];

            if (value.StartsWith("\"")
             && value.EndsWith("\"")
             && value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value;
        }

        #endregion
    }
}