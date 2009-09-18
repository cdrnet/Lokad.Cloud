#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud
{
	/// <summary>Simple non-sharded counter.</summary>
	/// <remarks>The content of the counter is stored in a single blob value.</remarks>
	public class BlobCounter
	{
		readonly IBlobStorageProvider _provider;

		readonly string _containerName;
		readonly string _blobName;

		/// <summary>Constant value provided for the cloud enumeration pattern
		/// over a queue.</summary>
		/// <remarks>The constant value is <c>2^48</c>, expected to be sufficiently
		/// large to avoid any arithmetic overflow with <c>long</c> values.</remarks>
		public const long Aleph = 2L << 48;

		/// <summary>Container that is storing the counter.</summary>
		public string ContainerName { get { return _containerName; } }

		/// <summary>Blob that is storing the counter.</summary>
		public string BlobName { get { return _blobName; } }

		/// <summary>Shorthand constructor.</summary>
		public BlobCounter(ProvidersForCloudStorage providers, string containerName, string blobName)
			: this(providers.BlobStorage, containerName, blobName)
		{
		}

		/// <summary>Shorthand constructor.</summary>
		public BlobCounter(IBlobStorageProvider provider, BaseBlobName fullName)
            : this(provider, fullName.ContainerName, fullName.ToString())
		{
		}

		/// <summary>Full constructor.</summary>
		public BlobCounter(IBlobStorageProvider provider, string containerName, string blobName)
		{
			Enforce.Argument(() => provider);
			Enforce.Argument(() => containerName);
			Enforce.Argument(() => blobName);

			_provider = provider;
			_containerName = containerName;
			_blobName = blobName;
		}

		/// <summary>Returns the value of the counter.</summary>
		public decimal GetValue()
		{
			return _provider.GetBlob<decimal>(_containerName, _blobName);
		}

		/// <summary>Atomic increment the counter value.</summary>
		/// <remarks>If the counter does not exist before hand, it gets created with a zero value.</remarks>
		public decimal Increment(decimal increment)
		{
			decimal counter;
			_provider.AtomicUpdate(_containerName, _blobName, x => x + increment, out counter);

			return counter;
		}

		/// <summary>Reset the counter at the given value.</summary>
		public void Reset(decimal value)
		{
			_provider.PutBlob(_containerName, _blobName, value);
		}

		/// <summary>Deletes the counter.</summary>
		/// <returns><c>true</c> if the counter has actually been deleted by the call,
		/// and <c>false</c> otherwise.</returns>
		public bool Delete()
		{
			return _provider.DeleteBlob(_containerName, _blobName);
		}
	}
}
