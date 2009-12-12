#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud
{
	/// <summary>Helper to facilitate blob storage enumeration.</summary>
	[Serializable, DataContract]
	public class BlobNamePrefix<T> where T : BaseBlobName
	{
		[DataMember] public string Container { get; set; }
		[DataMember] public string Prefix { get; set; }

		public BlobNamePrefix(string container, string prefix)
		{
			Container = container;
			Prefix = prefix;
		}
	}
}
