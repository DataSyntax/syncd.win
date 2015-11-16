using System;

namespace SyncD.Data.Exceptions
{
    public class SettingNotFoundException : Exception
    {
        public SettingNotFoundException(string message)
            : base(message)
        {

        }
    }
}