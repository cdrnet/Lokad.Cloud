#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

// HACK: we are currently assuming that the amount of garbage items is low.
// Yet, in some circumstances (service failures for example), the amount of overflowing
// messages could be large, and would need a more scalable implementation pattern.

// TODO: make name pattern here consistent with the TemporaryBlobName

namespace Lokad.Cloud.Services
{
	/// <summary>
	/// Garbage collects temporary items stored in the <see cref="CloudService.TemporaryContainer"/>.
	/// </summary>
	/// <remarks>
	/// The container <see cref="CloudService.TemporaryContainer"/> is handy to
	/// store non-persistent data, typically state information concerning ongoing
	/// processing.
	/// </remarks>
	[ScheduledServiceSettings(
		AutoStart = true, 
		Description = "Garbage collects temporary items.",
		TriggerInterval = 24 * 60 * 60)] // 1 execution per day by default
	public class GarbageCollectorService : ScheduledService
	{
		/// <remarks>Name is overriden for consistency in the framework.</remarks>
		public override string Name
		{
			get { return "lokad-cloud-garbage-collector"; }
		}

		protected override void StartOnSchedule()
		{
			const string cn = TemporaryContainer;

			// lazy enumeration over the overflowing messages
			foreach(var blobName in Providers.BlobStorage.List(cn, null))
			{
				var parsedName = BaseBlobName.Parse<TemporaryBlobName>(blobName);

				// if the overflowing message is expired, delete it
				if(DateTime.Now > parsedName.Expiration)
				{
					Providers.BlobStorage.DeleteBlob(cn, blobName);
				}
				else
				{
					// overflowing messages are iterated in date-increasing order
					// as soon a non-expired overflowing message is encountered
					// just stop the process.
					break;
				}
			}
		}
	}
}
