using System;

namespace SyncIt.Data.Exceptions
{
    public class SettingNotFoundException : Exception
    {
        public SettingNotFoundException(string message)
            : base(message)
        {

        }
    }
}