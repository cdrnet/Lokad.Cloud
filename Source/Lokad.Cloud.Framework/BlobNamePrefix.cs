#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

namespace Lokad.Cloud
{
	/// <typeparam name="T">Type of iterated items.</typeparam>
	[Serializable]
	public class BlobNamePrefix
	{
		public string Container { get; set; }
		public string Prefix { get; set; }

		public BlobNamePrefix(string container, string prefix)
		{
			Container = container;
			Prefix = prefix;
		}
	}
}
