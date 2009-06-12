#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Services
{
	/// <summary>Elementary mapping to be performed by the <see cref="BlobSetService"/>.</summary>
	[Serializable]
	public class BlobSetMessage
	{
		public string SourcePrefix { get; set; }

		public string DestinationPrefix { get; set; }

		public string BlobName { get; set; }
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
            foreach(var message in messages)
            {
            	throw new NotImplementedException();
            }
		}
	}
}
