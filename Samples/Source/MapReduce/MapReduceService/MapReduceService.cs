#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Samples.MapReduce
{
	/// <summary>Implements a map/reduce service.</summary>
	/// <seealso cref="MapReduceBlobSet"/>
	/// <seealso cref="MapReduceJob"/>
	[QueueServiceSettings(
		AutoStart = true,
		Description = "Processes map/reduce jobs",
		QueueName = MapReduceBlobSet.JobsQueueName)]
	public class MapReduceService : QueueService<JobMessage>
	{
		protected override void Start(JobMessage message)
		{
			switch(message.Type)
			{
				case MessageType.BlobSetToProcess:
					ProcessBlobSet(message.JobName, message.BlobSetId.Value);
					break;
				case MessageType.ReducedDataToAggregate:
					AggregateData(message.JobName);
					break;
				default:
					throw new InvalidOperationException("Invalid Message Type");
			}
		}

		void ProcessBlobSet(string jobName, int blobSetId)
		{
			var blobSet = new MapReduceBlobSet(BlobStorage, QueueStorage);
			blobSet.PerformMapReduce(jobName, blobSetId);
		}

		void AggregateData(string jobName)
		{
			var blobSet = new MapReduceBlobSet(BlobStorage, QueueStorage);
			blobSet.PerformAggregate(jobName);
		}

	}

}
