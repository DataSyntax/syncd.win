﻿using SyncIt.Data.Enumerations;

namespace SyncIt.Data.Concrete
{
    public class Settings
    {
        public string LogFileName { get; set; }
        public string PidFileName { get; set; }
        public string WatchCommand { get; set; }
        public string SyncCommand { get; set; }
        public Verbose Verbose { get; set; }
    }
}