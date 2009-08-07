#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Globalization;
using Lokad.Cloud.Framework;

// HACK: the delayed queue service does not provide a scalable iteration pattern.
// (one instance max iterating over the delayed message)

namespace Lokad.Cloud.Services
{
	/// <summary>Routinely checks for expired delayed messages that needs to
	/// be put in queue for immediate consumption.</summary>
	[ScheduledServiceSettings(
		AutoStart = true,
		Description = "Checks for expired delayed messages to be put in regular queue.",
		TriggerInterval = 15)] // 15s
	public class DelayedQueueService : ScheduledService
	{
		protected override void StartOnSchedule()
		{
			const string cn = DelayedMessageContainer;

			// lazy enumeration over the delayed messages
			foreach (var blobName in Providers.BlobStorage.List(cn, null))
			{
				// 24 because of custom datetime format, see below
				var prefix = blobName.Substring(0, 24);

				// Prefix pattern used for the storage is yyyy/MM/dd/...
				// The prefix is encoding the expiration date of the overflowing message.
				var expiration = DateTime.ParseExact(prefix, 
					"yyyy/MM/dd/hh/mm/ss/ffff", CultureInfo.InvariantCulture);

				// HACK: duplicated logic with the queue overflow collector

				// if the overflowing message is expired, delete it
				if (DateTime.Now > expiration)
				{
					var dm = Providers.BlobStorage.GetBlob<DelayedMessage>(cn, blobName);
					Providers.QueueStorage.Put(dm.QueueName, dm.InnerMessage);
					Providers.BlobStorage.DeleteBlob(cn, blobName);
				}
				else
				{
					// delayed messages are iterated in date-increasing order
					// as soon a non-expired delayed message is encountered
					// just stop the process.
					break;
				}
			}
		}
	}
}
