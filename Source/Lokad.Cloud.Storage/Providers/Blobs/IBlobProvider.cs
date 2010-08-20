#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cloud.Storage.Providers.Blobs
{
	public interface IBlobProvider
	{
		/// <returns>
		/// The task completes as soon as the creation of the container has been accepted
		/// without error. However, there can still be some delay until the container is actually
		/// available.
		/// </returns>
		[Idempotent]
		Task CreateContainerIfNotExists(string containerName, CancellationToken cancellationToken);

		/// <returns>
		/// The task completes as soon as the deletion of the container has been accepted
		/// without error. However, there can still be some delay until the container is actually
		/// unavailable.
		/// </returns>
		[Idempotent]
		Task DeleteContainerIfExists(string containerName, CancellationToken cancellationToken);
	}
}
