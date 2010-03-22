#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
	/// <summary>The purpose of the <see cref="MessageWrapper"/> is to gracefully
	/// handle messages that are too large of the queue storage (or messages that 
	/// happen to be already stored in the Blob Storage).</summary>
	[DataContract]
	internal sealed class MessageWrapper
	{
		[DataMember]
		public string ContainerName { get; set; }

		[DataMember]
		public string BlobName { get; set; }
	}
}