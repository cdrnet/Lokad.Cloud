#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		protected override void Start(JobMessage messages)
		{
			throw new NotImplementedException();
		}

	}

}
