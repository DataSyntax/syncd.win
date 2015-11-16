using System;

namespace SyncD.Data.Exceptions
{
    public class TooMuchArgsException : Exception
    {
        public TooMuchArgsException(string message)
            : base(message)
        {

        }
    }
}