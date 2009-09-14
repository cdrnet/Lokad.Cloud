#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud
{
	///<summary>Throw this exception to notify a crash of a cloud service.</summary>
	[Serializable]
	public class CloudServiceException : ApplicationException
	{
		/// <summary>Empty constructor.</summary>
		public CloudServiceException()
		{
		}

		/// <summary>Constructor with message.</summary>
		public CloudServiceException(string message)
			: base(message)
		{
		}

		/// <summary>Constructor with message and inner exception.</summary>
		public CloudServiceException(string message, Exception inner)
			: base(message, inner)
		{
		}

		/// <summary>Deserialization constructor.</summary>
		public CloudServiceException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
