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

	/// <summary>
	/// Implements a custom formatter for data serialization.
	/// </summary>
	/// <typeparam name="T">The type of object to serialize.</typeparam>
	/// <remarks>This class is not <b>thread-safe</b>.</remarks>
	public class CustomFormatter : ICustomFormatter
	{
		DataContractSerializer _serializer = null;
		Type _currentType;

		void CreateSerializerIfNecessary(Type type)
		{
			if(_serializer == null || _currentType != type) _serializer = new DataContractSerializer(type);
			_currentType = type;
		}

		public void Serialize(Stream destination, object instance)
		{
			CreateSerializerIfNecessary(instance.GetType());

			using(var compressed = destination.Compress(true))
			using(var writer = XmlDictionaryWriter.CreateBinaryWriter(compressed, null, null, false))
			//using(var writer = XmlDictionaryWriter.Create(XmlWriter.Create(destination), new XmlWriterSettings() { CloseOutput = false }))
			{
				_serializer.WriteObject(writer, instance);
			}
		}

		public object Deserialize(Stream source, Type type)
		{
			CreateSerializerIfNecessary(type);

			using(var decompressed = source.Decompress(true))
			using(var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
			//using(var reader = XmlDictionaryReader.Create(XmlReader.Create(source), new XmlReaderSettings() { CloseInput = false }))
			{
				return _serializer.ReadObject(reader);
			}
		}

	}
}
