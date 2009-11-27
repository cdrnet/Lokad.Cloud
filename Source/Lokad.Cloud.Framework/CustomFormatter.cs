#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;

namespace Lokad.Cloud
{
	/// <summary>Defines the interface for a custom formatter.</summary>
	public interface ICustomFormatter
	{
		/// <summary>
		/// Serializes an object to a stream.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="destination">The destination stream.</param>
		/// <param name="instance">The object.</param>
		void Serialize<T>(Stream destination, T instance);

		/// <summary>
		/// Deserializes an object from a stream.
		/// </summary>
		/// <typeparam name="T">The type of the object.</typeparam>
		/// <param name="source">The source stream.</param>
		/// <returns>The deserialized object.</returns>
		T Deserialize<T>(Stream source);
	}

	/// <summary>
	/// Implements a custom formatter for data serialization.
	/// </summary>
	/// <typeparam name="T">The type of object to serialize.</typeparam>
	/// <remarks>This class is not <b>thread-safe</b>.</remarks>
	public class CustomFormatter : ICustomFormatter
	{
		DataContractSerializer _serializer = null;
		Type _currentType;

		void CreateSerializerIfNecessary<T>()
		{
			if(_serializer == null || _currentType != typeof(T)) _serializer = new DataContractSerializer(typeof(T));
			_currentType = typeof(T);
		}

		public void Serialize<T>(Stream destination, T instance)
		{
			CreateSerializerIfNecessary<T>();

			using(var compressed = destination.Compress(true))
			using(var writer = XmlDictionaryWriter.CreateBinaryWriter(compressed, null, null, false))
			//using(var writer = XmlDictionaryWriter.Create(XmlWriter.Create(destination), new XmlWriterSettings() { CloseOutput = false }))
			{
				_serializer.WriteObject(writer, instance);
			}
		}

		public T Deserialize<T>(Stream source)
		{
			CreateSerializerIfNecessary<T>();

			using(var decompressed = source.Decompress(true))
			using(var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
			//using(var reader = XmlDictionaryReader.Create(XmlReader.Create(source), new XmlReaderSettings() { CloseInput = false }))
			{
				return (T)_serializer.ReadObject(reader);
			}
		}

	}
}
