#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Services
{
	[Serializable]
	public class BlobSetReduceMessage
	{
		public string SourcePrefix { get; set; }

		public string ReductionSettings { get; set; }
	}

	/// <summary>Framework service part of Lokad.Cloud. This service is used to
	/// perform reduce operations starting from a <see cref="BlobSet{T}"/>.</summary>
	[QueueServiceSettings(AutoStart = true, QueueName = QueueName)]
	public class BlobSetReduceService : QueueService<BlobSetReduceMessage>
	{
		public const string QueueName = "lokad-blobsets-reduce";

		public BlobSetReduceService(ProvidersForCloudStorage providers) : base(providers)
		{
		}

		protected override void Start(IEnumerable<BlobSetReduceMessage> messages)
		{
			foreach(var message in messages)
			{
				// TODO: implementation missing

				Delete(message);
			}

			throw new NotImplementedException();
		}


	}
}
