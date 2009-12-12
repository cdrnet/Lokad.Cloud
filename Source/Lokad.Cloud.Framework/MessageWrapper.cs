#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud
{
	/// <summary>The purpose of the <see cref="MessageWrapper"/> is to gracefully
	/// handle messages that are too large of the queue storage (or messages that 
	/// happen to be already stored in the Blob Storage).</summary>
	[Serializable, DataContract]
	internal sealed class MessageWrapper : ISerializable
	{
		[DataMember] public string ContainerName { get; set; }

		[DataMember] public string BlobName { get; set; }

		public MessageWrapper()
		{
		}

		/// <summary>Deserialization constructor.</summary>
		public MessageWrapper(SerializationInfo info, StreamingContext context)
		{
			ContainerName = info.GetString("cn");
			BlobName = info.GetString("bn");
		}

		/// <summary>Serialization method.</summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("vs", "1.0");
			info.AddValue("cn", ContainerName);
			info.AddValue("bn", BlobName);
		}
	}
}
