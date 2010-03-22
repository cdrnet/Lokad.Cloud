#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Quality;

namespace Lokad.Cloud.Storage
{
	/// <summary>Used to specify the field position in the blob name.</summary>
	/// <remarks>The name (chosen as the abbreviation of "field position")
	/// is made compact not to make client code too verbose.</remarks>
	public class RankAttribute : Attribute
	{
		[UsedImplicitly] public readonly int Index;

		/// <summary>Position v
		/// </summary>
		/// <param name="index"></param>
		public RankAttribute(int index)
		{
			Index = index;
		}
	}
}