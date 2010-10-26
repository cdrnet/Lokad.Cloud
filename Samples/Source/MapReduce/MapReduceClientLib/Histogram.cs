#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Samples.MapReduce
{
	/// <summary>Represents a picture histogram.</summary>
	[DataContract]
	public class Histogram
	{
		/// <summary>The size of the <see cref="M:Frequencies"/> array (2^8).</summary>
		public static readonly int FrequenciesSize = 256;

		/// <summary>An array of 256 items, each representing 
		/// the frequency of each brightness level (0.0 to 1.0).</summary>
		[DataMember]
		public double[] Frequencies;

		/// <summary>The total number of pixels (weights the histogram).</summary>
		[DataMember]
		public int TotalPixels;

		protected Histogram() { }

		/// <summary>Initializes a new instance of the <see cref="T:Histogram"/> class.</summary>
		/// <param name="totalPixels">The total number of pixels.</param>
		public Histogram(int totalPixels)
		{
			Frequencies = new double[FrequenciesSize];
			TotalPixels = totalPixels;
		}

		/// <summary>Gets the max frequency in the histogram (for scaling purposes).</summary>
		/// <returns>The max frequency.</returns>
		public double GetMaxFrequency()
		{
			return Frequencies.Max();
		}

	}

}
