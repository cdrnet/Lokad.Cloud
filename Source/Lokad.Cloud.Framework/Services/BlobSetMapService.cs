#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Framework;

using BlobSet = Lokad.Cloud.Framework.BlobSet<object>;

// TODO: need to use a custom queue
// TODO: need to support caching for the mapper

namespace Lokad.Cloud.Services
{
	/// <summary>Elementary mapping to be performed by the <see cref="BlobSetMapService"/>.</summary>
	[Serializable]
	public class BlobSetMapMessage
	{
		/// <summary>Prefix associated to the input <c>BlobSet</c>.</summary>
		public string SourcePrefix { get; set; }

		/// <summary>Prefix associated to the output <c>BlobSet</c>.</summary>
		public string DestinationPrefix { get; set; }

		/// <summary>Suffix associated to the item being considered.</summary>
		public string ItemSuffix { get; set; }
	}

	/// <summary>Framework service part of Lokad.Cloud. This service is used to
	/// perform map operations between <see cref="BlobSet{T}"/>.</summary>
	[QueueServiceSettings(AutoStart = true, QueueName = QueueName,
		Description = "Performs Map operations on BlobSets.")]
	public class BlobSetMapService : QueueService<BlobSetMapMessage>
	{
		public const string QueueName = "lokad-cloud-blobsets-map";

		protected override void StartRange(IEnumerable<BlobSetMapMessage> messages)
		{
			var blobStorage = Providers.BlobStorage; // short-hand

			const string mapSettingsSuffix = BlobSet.MapSettingsSuffix;
			const string mapCounterSuffix = BlobSet.MapCounterSuffix;

            foreach(var message in messages)
            {
            	var settingsBlobName = new BlobSetMapName(message.DestinationPrefix, mapSettingsSuffix);
            	var counterBlobName = new BlobSetMapName(message.DestinationPrefix, mapCounterSuffix);
            	var inputBlobName = new BlobSetMapName(message.SourcePrefix, message.ItemSuffix);
            	var outputBlobName = new BlobSetMapName(message.DestinationPrefix, message.ItemSuffix);

                // retrieving the mapper
            	var mapSettings = blobStorage.GetBlob<BlobSetMapSettings>(settingsBlobName);

				// retrieving the input
            	var input = blobStorage.GetBlob<object>(inputBlobName);

				// map
            	var output = BlobSet.InvokeAsDelegate(mapSettings.Mapper, input);

				// saving the output
				blobStorage.PutBlob(outputBlobName, output);

				// Decrementing the counter once the operation is completed
            	var counter = new BlobCounter(blobStorage, counterBlobName);
            	var remainingMappings = (long) counter.Increment(-1);

				// deleting message
				Delete(message);

				// HACK: failing processes could generate retry, and eventually negative values here.
				if(remainingMappings <= 0)
				{
					counter.Delete();

					// pushing the message as a completion signal
					Providers.QueueStorage.Put(
						mapSettings.OnCompletedQueueName, mapSettings.OnCompleted);
				}
            }
		}
	}
}
