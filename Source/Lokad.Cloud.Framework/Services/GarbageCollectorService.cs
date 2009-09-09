#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

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
		TriggerInterval = 60)] // 1 execution every 1min
	public class GarbageCollectorService : ScheduledService
	{
		static TimeSpan MaxExecutionTime { get { return 10.Minutes(); } }

		/// <remarks>Name is overriden for consistency in the framework.</remarks>
		public override string Name
		{
			get { return "lokad-cloud-garbage-collector"; }
		}

		protected override void StartOnSchedule()
		{
			var executionExpiration = DateTime.UtcNow.Add(MaxExecutionTime);

			// lazy enumeration over the overflowing messages
			foreach (var blobName in BlobStorage.List(
				BaseBlobName.GetContainerName<TemporaryBlobName>(), null))
			{
				var parsedName = BaseBlobName.Parse<TemporaryBlobName>(blobName);

				// if the overflowing message is expired, delete it
				if(DateTime.UtcNow > parsedName.Expiration)
				{
					BlobStorage.DeleteBlob(parsedName);
				}
				else
				{
					// overflowing messages are iterated in date-increasing order
					// as soon a non-expired overflowing message is encountered
					// just stop the process.
					break;
				}

				// don't freeze the worker with this service
				if (DateTime.UtcNow > executionExpiration)
				{
					break;
				}
			}
		}
	}
}
