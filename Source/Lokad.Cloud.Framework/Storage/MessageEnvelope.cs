#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
	/// <summary>
	/// The purpose of the <see cref="MessageEnvelope"/> is to provide
	/// additional metadata for a message.
	/// </summary>
	[DataContract]
	internal sealed class MessageEnvelope
	{
		[DataMember]
		public int DequeueCount { get; set; }

		[DataMember]
		public byte[] RawMessage { get; set; }
	}
}