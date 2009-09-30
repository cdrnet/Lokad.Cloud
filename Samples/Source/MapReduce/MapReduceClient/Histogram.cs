#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapReduceClient
{
	/// <summary>Represents a picture histogram.</summary>
	public class Histogram
	{
		/// <summary>An array of 256 items, each representing 
		/// the frequency of each brightness level (0.0 to 1.0).</summary>
		public float[] Frequencies;
	}

}
