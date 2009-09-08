#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Linq;
using Lokad.Cloud;
using BlobSet = MapReduce.BlobSet<object>;

// TODO: a smart reduction process would take into account the size and CPU cost
// of the reduction to figure out how many item needs to be retrieved at once

// TODO: need to migrate toward BaseBlogName

namespace MapReduce
{
	/// <summary>Message for an elementary map operation in a map-reduce process.</summary>
	[Serializable]
	public class BlobSetReduceMessage
	{
		/// <summary>Source blobset used as input.</summary>
		public string SourcePrefix { get; set; }

		/// <summary>Blob settings suffix.</summary>
		public string SettingsSuffix { get; set; }
	}

	/// <summary>Framework service part of Lokad.Cloud. This service is used to
	/// perform reduce operations starting from a <see cref="BlobSet{T}"/>.</summary>
	[QueueServiceSettings(AutoStart = true, QueueName = QueueName, BatchSize = 1,
		Description = "Perfoms reduce operation on BlobSets.")]
	public class BlobSetReduceService : QueueService<BlobSetReduceMessage>
	{
		public const string QueueName = "lokad-cloud-blobsets-reduce";

		protected override void Start(BlobSetReduceMessage message)
		{
			const string containerName = BlobSet.ContainerName;
			const string delimiter = BlobSet.Delimiter;

			var settingsBlobName = message.SourcePrefix + delimiter + message.SettingsSuffix;

			var settings = BlobStorage.GetBlob<BlobSetReduceSettings>(containerName, settingsBlobName);

			// cleanup has already been performed, reduction is complete.
			if (null == settings)
			{
				Delete(message);
				return;
			}

			var remainingReductions = long.MaxValue;
			var counterBlobName = message.SourcePrefix + delimiter + settings.ReductionCounter;
			var counter = new BlobCounter(Providers, containerName, counterBlobName);

			var items = QueueStorage.Get<object>(settings.WorkQueue, 2);

			// if there are at least two items, then reduce them
			if (items.Count() >= 2)
			{
				var current = items.First();
				var next = items.Skip(1).First();

				while (next != null)
				{
					var reducted = BlobSet.InvokeAsDelegate(settings.Reducer, current, next);

					QueueStorage.Put(settings.WorkQueue, reducted);
					QueueStorage.DeleteRange(settings.WorkQueue, new[] { current, next });

					remainingReductions = (long)counter.Increment(-1);

					current = reducted;

					// HACK: if the items are small, we would need to retrieve them
					// in batches here.

					// retrieving the next item and keep up with the reduction
					var nextItems = QueueStorage.Get<object>(settings.WorkQueue, 1);
					next = nextItems.Any() ? nextItems.First() : null;
				}

				// iteration beyond zero are possible through rare condition
				if (remainingReductions <= 0)
				{
					QueueStorage.Put(settings.ReductionQueue, current);

					// performing cleanup
					BlobStorage.DeleteBlob(containerName, settingsBlobName);
					counter.Delete();
					QueueStorage.DeleteQueue(settings.WorkQueue);
				}
			}
			else
			{
				// not enough items retrieved for reduction
				remainingReductions = BlobStorage.GetBlob<long>(containerName, counterBlobName);
			}

			// reduction is still under way
			if (remainingReductions > 0)
			{
				// same message is queued again, for later processing.
				Put(message, QueueName);
			}

			Delete(message);
		}
	}
}
