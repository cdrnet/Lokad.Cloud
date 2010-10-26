#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

// IDEA: the message of the exception could be logged in to the cloud logs.
// (issue: how to avoid N identical messages to be logged through all workers)

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	///<summary>Throw this exception in order to force a worker restart.</summary>
	[Serializable]
	public class TriggerRestartException : ApplicationException
	{
		/// <summary>Empty constructor.</summary>
		public TriggerRestartException()
		{
		}

		/// <summary>Constructor with message.</summary>
		public TriggerRestartException(string message) : base(message)
		{
		}

		/// <summary>Constructor with message and inner exception.</summary>
		public TriggerRestartException(string message, Exception inner) : base(message, inner)
		{	
		}

		/// <summary>Deserialization constructor.</summary>
		public TriggerRestartException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}