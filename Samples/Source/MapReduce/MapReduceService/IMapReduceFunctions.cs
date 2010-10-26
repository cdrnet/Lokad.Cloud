
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Samples.MapReduce
{
	/// <summary>
	/// Defines the interface for a class that implements map/reduce functions.
	/// </summary>
	/// <remarks>Classes implementing this interface must have a parameterless constructor,
	/// and must be deployed both on server and client.</remarks>
	public interface IMapReduceFunctions
	{
		/// <summary>Gets the mapper function, expected to be Func{TMapIn, TMapOut}.</summary>
		/// <returns>The mapper function.</returns>
		object GetMapper();

		/// <summary>Gets the reducer function, expected to be Func{TMapOut, TMapOut, TMapOut}.</summary>
		/// <returns>The reducer function.</returns>
		object GetReducer();
	}
}
