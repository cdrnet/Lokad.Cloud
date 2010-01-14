#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
	/// <summary>Entity to be stored by the <see cref="ITableStorageProvider"/>.</summary>
	/// <typeparam name="T">Type of the value carried by the entity.</typeparam>
	public class CloudEntity<T>
	{
		/// <summary>Indexed system property.</summary>
		public string RowRey { get; set; }

		/// <summary>Indexed system property.</summary>
		public string PartitionKey { get; set; }

		/// <summary>Flag indicating last update.</summary>
		public DateTime Timestamp { get; set; }

		/// <summary>Value carried by the entity.</summary>
		public T Value { get; set; }
	}
}
