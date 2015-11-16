using System;

namespace SyncD.Data.Exceptions
{
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException(string message)
            : base(message)
        {

        }
    }
}