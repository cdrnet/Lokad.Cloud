#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Samples.MapReduce {

	/// <summary>Contains functions for map/reduce.</summary>
	[Serializable]
	public class MapReduceFunctions
	{

		/// <summary>The mapper, expected to be Func{TMapIn, TMapOut}.</summary>
		public object Mapper { get; set; }

		/// <summary>The reducer, expected to be Func{TMapOut, TMapOut, TMapOut}.</summary>
		public object Reducer { get; set; }

	}

}
