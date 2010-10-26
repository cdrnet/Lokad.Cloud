
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Samples.MapReduce
{
	/// <summary>
	/// Implements map/reduce functions for the Histogram sample.
	/// </summary>
	public class HistogramMapReduceFunctions : IMapReduceFunctions
	{
		public object GetMapper()
		{
			return (Func<byte[], Histogram>)Helpers.ComputeHistogram;
		}

		public object GetReducer()
		{
			return (Func<Histogram, Histogram, Histogram>)Helpers.MergeHistograms;
		}
	}
}
