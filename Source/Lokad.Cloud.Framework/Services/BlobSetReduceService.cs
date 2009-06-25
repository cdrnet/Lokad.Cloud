#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Framework;

using BlobSet = Lokad.Cloud.Framework.BlobSet<object>;

// TODO: a smart reduction process would take into account the size and CPU cost
// of the reduction to figure out how many item needs to be retrieved at once

namespace Lokad.Cloud.Services
{
	[Serializable]
	public class BlobSetReduceMessage
	{
		public string SourcePrefix { get; set; }

		public string SettingsSuffix { get; set; }
	}

	/// <summary>Framework service part of Lokad.Cloud. This service is used to
	/// perform reduce operations starting from a <see cref="BlobSet{T}"/>.</summary>
	[QueueServiceSettings(AutoStart = true, QueueName = QueueName, BatchSize = 1)]
	public class BlobSetReduceService : QueueService<BlobSetReduceMessage>
	{
		public const string QueueName = "lokad-cloud-blobsets-reduce";

		public BlobSetReduceService(ProvidersForCloudStorage providers) : base(providers)
		{
		}

		protected override void Start(IEnumerable<BlobSetReduceMessage> messages)
		{
			const string containerName = BlobSet.ContainerName;
			const string delimiter = BlobSet.Delimiter;

			foreach(var message in messages)
			{
				var settingsBlobName = message.SourcePrefix + delimiter + message.SettingsSuffix;
				
				var settings = _providers.BlobStorage.
					GetBlob<BlobSetReduceSettings>(containerName, settingsBlobName);

				// cleanup has already been performed, reduction is complete.
				if(null == settings)
				{
					Delete(message);
					continue;
				}

				var remainingReductions = long.MaxValue;

				var reductionCounterBlobName = message.SourcePrefix + delimiter + settings.ReductionCounter;

				var items = _providers.QueueStorage.Get<object>(settings.WorkQueue, 2);

				// if there are at least two items, then reduce them
				if (items.Count() >= 2)
				{
					var current = items.First();
					var next = items.Skip(1).First();

					while (next != null)
					{
						var reducted = BlobSet.InvokeAsDelegate(settings.Reducer, current, next);

						_providers.QueueStorage.Put(settings.WorkQueue, new[] {reducted});
						_providers.QueueStorage.Delete(settings.WorkQueue, new []{current, next});

						BlobSet.RetryUpdate(() => _providers.BlobStorage.UpdateIfNotModified(
							containerName,
							reductionCounterBlobName,
							x => x - 1,
							out remainingReductions));

						current = reducted;

						// HACK: if the items are small, we would need to retrieve them
						// in batches here.

						// retrieving the next item and keep up with the reduction
						var nextItems = _providers.QueueStorage.Get<object>(settings.WorkQueue, 1);
						next = nextItems.Any() ? nextItems.First() : null;
					}

					if (remainingReductions == 0)
					{
						_providers.QueueStorage.Put(settings.ReductionQueue, new[] { current });

						// performing cleanup
						_providers.BlobStorage.DeleteBlob(containerName, settingsBlobName);
						_providers.BlobStorage.DeleteBlob(containerName, reductionCounterBlobName);
						_providers.QueueStorage.DeleteQueue(settings.WorkQueue);
					}
				}
				else
				{
					// not enough items retrieved for reduction
					remainingReductions = 
						_providers.BlobStorage.GetBlob<long>(containerName, reductionCounterBlobName);
                }

				// reduction is still under way
				if (remainingReductions > 0)
				{
					// same message is queued again, for later processing.
					Put(new[] { message }, QueueName);
				}

				Delete(message);
			}
		}
	}
}
