#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Runtime.Serialization;
using Lokad.Cloud.Core;

namespace Lokad.Cloud.Framework
{
	/// <summary>IoC argument for <see cref="CloudService"/>.</summary>
	/// <remarks>This argument will be populated through Inversion Of Control (IoC)
	/// by the Lokad.Cloud framework itself. This class is placed in the
	/// <c>Lokad.Cloud.Framework</c> for convenience while inheriting a
	/// <see cref="CloudService"/>.</remarks>
	public class ProvidersForCloudService
	{
		/// <summary>Error Logger</summary>
		public ILog Log { get; set; }

		/// <summary>Type mapper for implicit cloud storage.</summary>
		public ITypeMapperProvider TypeMapper { get; set; }

		/// <summary>Abstracts the Queue Storage.</summary>
		public IQueueStorageProvider QueueStorage { get; set; }
	}
}
