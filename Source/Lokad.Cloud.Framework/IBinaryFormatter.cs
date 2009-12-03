#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;

namespace Lokad.Cloud
{
	/// <summary>Defines the interface for a custom formatter.</summary>
	public interface IBinaryFormatter
	{
		/// <summary>Serializes an object to a stream.</summary>
		/// <param name="destination">The destination stream.</param>
		/// <param name="instance">The object.</param>
		void Serialize(Stream destination, object instance);

		/// <summary>Deserializes an object from a stream.</summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="source">The source stream.</param>
		/// <param name="type">The type of the object.</param>
		/// <returns>The deserialized object.</returns>
		object Deserialize(Stream source, Type type);
	}
}
