﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Lokad.Serialization;

namespace Lokad.Cloud.Storage
{
	/// <summary>
	/// Formatter based on <c>DataContractSerializer</c> and <c>NetDataContractSerializer</c>. 
	/// The formatter targets storage of persistent or transient data in the cloud storage.
	/// </summary>
	/// <remarks>
	/// If a <c>DataContract</c> attribute is present, then the <c>DataContractSerializer</c>
	/// is favored. If not, then the <c>NetDataContractSerializer</c> is used instead.
	/// This class is not <b>thread-safe</b>.
	/// </remarks>
	public class CloudFormatter : IDataSerializer, IIntermediateDataSerializer
	{
		XmlObjectSerializer GetXmlSerializer(Type type)
		{
			if (type.GetAttributes<DataContractAttribute>(false).Length > 0)
			{
				return new DataContractSerializer(type);
			}
			
			return new NetDataContractSerializer();
		}

		public void Serialize(object instance, Stream destination)
		{
			var serializer = GetXmlSerializer(instance.GetType());

			using(var compressed = destination.Compress(true))
			using(var writer = XmlDictionaryWriter.CreateBinaryWriter(compressed, null, null, false))
			{
				serializer.WriteObject(writer, instance);
			}
		}

		public object Deserialize(Stream source, Type type)
		{
			var serializer = GetXmlSerializer(type);

			using(var decompressed = source.Decompress(true))
			using(var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
			{
				return serializer.ReadObject(reader);
			}
		}

		public XElement UnpackXml(Stream source)
		{
			using(var decompressed = source.Decompress(true))
			using (var reader = XmlDictionaryReader.CreateBinaryReader(decompressed, XmlDictionaryReaderQuotas.Max))
			{
				return XElement.Load(reader);
			}
		}

		public void RepackXml(XElement data, Stream destination)
		{
			using(var compressed = destination.Compress(true))
			using(var writer = XmlDictionaryWriter.CreateBinaryWriter(compressed, null, null, false))
			{
				data.Save(writer);
				writer.Flush();
				compressed.Flush();
			}
		}
	}
}