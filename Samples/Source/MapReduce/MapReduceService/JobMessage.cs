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

	/// <summary>Contains information about a batch item to process.</summary>
	[Serializable]
	public sealed class JobMessage
	{

		/// <summary>The type of the message.</summary>
		public MessageType Type { get; set; }

		/// <summary>The name of the job.</summary>
		public string JobName { get; set; }

		/// <summary>The ID of the blobset to process, if appropriate.</summary>
		public int? BlobSetId { get; set; }

	}

	/// <summary>Defines message types for <see cref="T:BatchMessage"/>s.</summary>
	public enum MessageType
	{
		/// <summary>A blob set must be processed (map/reduce).</summary>
		BlobSetToProcess,
		/// <summary>The result of a map/reduce job is ready for aggregation.</summary>
		ReducedDataToAggregate
	}

}
