#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using Lokad.Cloud.Framework;

using BlobSet = Lokad.Cloud.Framework.BlobSet<object>;

namespace Lokad.Cloud.Services
{
	/// <summary>Elementary mapping to be performed by the <see cref="BlobSetService"/>.</summary>
	[Serializable]
	public class BlobSetMessage
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
	[QueueServiceSettings(AutoStart = true, QueueName = QueueName)]
	public class BlobSetService : QueueService<BlobSetMessage>
	{
		public const string QueueName = "lokad-blobsets";

		public BlobSetService(ProvidersForCloudStorage providers) : base(providers)
		{
		}

		public override void Start(IEnumerable<BlobSetMessage> messages)
		{
			const string containerName = BlobSet.ContainerName;
			const string delimiter = BlobSet.Delimiter;
			const string mapSettingsBlobName = BlobSet.MapSettingsBlobName;
			const string mapCounterBlobName = BlobSet.MapCounterBlobName;

            foreach(var message in messages)
            {
            	var srcPrefix = message.SourcePrefix;
            	var destPrefix = message.DestinationPrefix;
            	var itemSuffix = message.ItemSuffix;

				// TODO: need to support caching for the mapper
                // retrieving the mapper
            	var mapSettings = _providers.BlobStorage.GetBlob<BlobSetMapSettings>(
            		containerName, srcPrefix + delimiter + mapSettingsBlobName);

				// retrieving the input
            	var input = _providers.BlobStorage.GetBlob<object>(
					containerName, srcPrefix + delimiter + itemSuffix);

				// invoking the mapper through reflexion
            	var output = mapSettings.Mapper.GetType().InvokeMember(
            		"Invoke", BindingFlags.InvokeMethod, null, mapSettings.Mapper, new[] {input});

				// saving the mapped output
				_providers.BlobStorage.PutBlob(
					containerName, destPrefix + delimiter + itemSuffix, output);

				// Decrementing the counter once the operation is completed
            	var remainingMappings = long.MaxValue;
				BlobSet.RetryUpdate(() => _providers.BlobStorage.UpdateIfNotModified(
					containerName, 
					destPrefix + delimiter + mapCounterBlobName, 
					x => x - 1, 
					out remainingMappings));

				// HACK: failing processes could generate retry, and eventually negative values here.
				if(remainingMappings == 0)
				{
					// pushing the message as a completion signal
					_providers.QueueStorage.Put(
						mapSettings.OnCompletedQueueName, new[]{mapSettings.OnCompleted});
				}
            }
		}
	}
}
