#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;

namespace Lokad.Cloud
{


	/// <summary>
	/// Implements a custom formatter for data serialization.  The formatter
	/// targets storage of persistent or transcient data in the cloud storage.
	/// </summary>
	/// <typeparam name="T">The type of object to serialize.</typeparam>
	/// <remarks>This class is not <b>thread-safe</b>.</remarks>
	public class CloudFormatter : IBinaryFormatter
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
			{
				_serializer.WriteObject(writer, instance);
			}
		}

		public object Deserialize(Stream source, Type type)
		{
			CreateSerializerIfNecessary(type);

			using(var decompressed = source.Decompress(true))
			using(var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
			{
				return _serializer.ReadObject(reader);
			}
		}

	}
}
