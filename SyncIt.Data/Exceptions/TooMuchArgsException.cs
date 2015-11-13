using System;

namespace SyncIt.Data.Exceptions
{
    public class TooMuchArgsException : Exception
    {
        public TooMuchArgsException(string message)
            : base(message)
        {

        }
    }
}