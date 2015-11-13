﻿using System;

namespace SyncIt.Data.Exceptions
{
    public class InvalidArgumentException : Exception
    {
        public InvalidArgumentException(string message)
            : base(message)
        {

        }
    }
}