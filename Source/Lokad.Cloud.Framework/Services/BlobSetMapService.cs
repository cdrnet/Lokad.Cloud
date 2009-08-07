#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Framework;

using BlobSet = Lokad.Cloud.Framework.BlobSet<object>;

// TODO: need to use a custom queue

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

		protected override void Start(IEnumerable<BlobSetMapMessage> messages)
		{
			const string containerName = BlobSet.ContainerName;
			const string delimiter = BlobSet.Delimiter;
			const string mapSettingsSuffix = BlobSet.MapSettingsSuffix;
			const string mapCounterSuffix = BlobSet.MapCounterSuffix;

            foreach(var message in messages)
            {
            	var settingsBlobName = message.DestinationPrefix + delimiter + mapSettingsSuffix;
            	var counterBlobName = message.DestinationPrefix + delimiter + mapCounterSuffix;
            	var inputBlobName = message.SourcePrefix + delimiter + message.ItemSuffix;
            	var outputBlobName = message.DestinationPrefix + delimiter + message.ItemSuffix;

				// TODO: need to support caching for the mapper
                // retrieving the mapper
            	var mapSettings = Providers.BlobStorage.
					GetBlob<BlobSetMapSettings>(containerName, settingsBlobName);

				// retrieving the input
            	var input = Providers.BlobStorage.GetBlob<object>(containerName, inputBlobName);

				// map
            	var output = BlobSet.InvokeAsDelegate(mapSettings.Mapper, input);

				// saving the output
				Providers.BlobStorage.PutBlob(containerName, outputBlobName, output);

				// Decrementing the counter once the operation is completed
            	var remainingMappings = long.MaxValue;
				BlobSet.RetryUpdate(() => Providers.BlobStorage.UpdateIfNotModified(
					containerName,
					counterBlobName, 
					x => x - 1, 
					out remainingMappings));

				// deleting message
				DeleteRange(new[]{message});

				// HACK: failing processes could generate retry, and eventually negative values here.
				if(remainingMappings == 0)
				{
					// pushing the message as a completion signal
					Providers.QueueStorage.PutRange(
						mapSettings.OnCompletedQueueName, new[]{mapSettings.OnCompleted});
				}
            }
		}
	}
}
