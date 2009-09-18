#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
	/// <summary>Maps types to storage names, and vice-versa.</summary>
	/// <remarks>
	/// Spec on queue names: http://msdn.microsoft.com/en-us/library/dd179349.aspx
	/// Spec on container names: http://msdn.microsoft.com/en-us/library/dd135715.aspx
	/// </remarks>
	public static class TypeMapper
	{
		public static string GetStorageName(Type type)
		{
			var name = type.FullName.ToLowerInvariant().Replace(".", "-");

			// TODO: need a smarter behavior with long type name.
			if(name.Length > 63)
			{
				throw new ArgumentOutOfRangeException("type", "Type name is too long for auto-naming.");
			}

			return name;
		}
	}
}