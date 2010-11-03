#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage.Azure
{
    /// <summary>
    /// Exception indicating that received data has been detected to be corrupt or altered.
    /// </summary>
    [Serializable]
    public class DataCorruptionException : Exception
    {
        public DataCorruptionException() { }

        public DataCorruptionException(string message) : base(message) { }

        public DataCorruptionException(string message, Exception inner) : base(message, inner) { }

        protected DataCorruptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
