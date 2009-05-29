#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Core
{
	/// <summary>The purpose of the <see cref="MessageWrapper"/> is to gracefully
	/// handle messages that are too large of the queue storage.</summary>
	[Serializable]
	class MessageWrapper : ISerializable
	{
		public object InnerMessage { get; set; }

		public bool IsOverflow { get; set; }

		public string ContainerName { get; set; }

		public string BlobName { get; set; }

		public MessageWrapper()
		{
		}

		/// <summary>Deserialization constructor.</summary>
		public MessageWrapper(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}

		/// <summary>Serialization method.</summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}
	}
}
