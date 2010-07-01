#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Cloud.Storage.Azure;
using Lokad.Cloud.Test;
using Lokad.Serialization;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test
{
	[TestFixture]
	public class FatEntityTests
	{
		[DataContract]
		public class TimeValue
		{
			[DataMember] public DateTime Time { get; set;}
			[DataMember] public double Value { get; set;}
		}

		[DataContract]
		public class TimeSerie
		{
			[DataMember] public TimeValue[] TimeValues { get; set;}
		}

		[Serializable]
		public class TimeValueNoContract
		{
			public DateTime Time { get; set; }
			public double Value { get; set; }
		}

		[Serializable]
		public class TimeSerieNoContract
		{
			public TimeValueNoContract[] TimeValues { get; set; }
		}

		readonly IDataSerializer _serializer = GlobalSetup.Container.Resolve<IDataSerializer>();

		[Test]
		public void Convert()
		{
			var timevalues = new TimeValue[20000];
			for(int i = 0; i < timevalues.Length; i++)
			{
				timevalues[i] = new TimeValue {Time = new DateTime(2001, 1, 1).AddMinutes(i), Value = i};
			}

			var serie = new TimeSerie {TimeValues = timevalues};

			var cloudEntity = new CloudEntity<TimeSerie>
				{
					PartitionKey = "part",
					RowKey = "key",
					Value = serie
				};

			var fatEntity = FatEntity.Convert(cloudEntity, _serializer);
			var cloudEntity2 = FatEntity.Convert<TimeSerie>(fatEntity, _serializer, null);
			var fatEntity2 = FatEntity.Convert(cloudEntity2, _serializer);

			Assert.IsNotNull(cloudEntity2);
			Assert.IsNotNull(fatEntity2);

			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity.PartitionKey);
			Assert.AreEqual(cloudEntity.RowKey, fatEntity.RowKey);
			

			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity2.PartitionKey);
			Assert.AreEqual(cloudEntity.RowKey, fatEntity2.RowKey);

			Assert.IsNotNull(cloudEntity2.Value);
			Assert.AreEqual(cloudEntity.Value.TimeValues.Length, cloudEntity2.Value.TimeValues.Length);

			for(int i = 0; i < timevalues.Length; i++)
			{
				Assert.AreEqual(cloudEntity.Value.TimeValues[i].Time, cloudEntity2.Value.TimeValues[i].Time);
				Assert.AreEqual(cloudEntity.Value.TimeValues[i].Value, cloudEntity2.Value.TimeValues[i].Value);
			}

			var data1 = fatEntity.GetData();
			var data2 = fatEntity2.GetData();
			Assert.AreEqual(data1.Length, data2.Length);
			for(int i = 0; i < data2.Length; i++)
			{
				Assert.AreEqual(data1[i], data2[i]);
			}
		}

		[Test]
		public void ConvertNoContract()
		{
			var timevalues = new TimeValueNoContract[20000];
			for (int i = 0; i < timevalues.Length; i++)
			{
				timevalues[i] = new TimeValueNoContract { Time = new DateTime(2001, 1, 1).AddMinutes(i), Value = i };
			}

			var serie = new TimeSerieNoContract { TimeValues = timevalues };

			var cloudEntity = new CloudEntity<TimeSerieNoContract>
			{
				PartitionKey = "part",
				RowKey = "key",
				Value = serie
			};

			var fatEntity = FatEntity.Convert(cloudEntity, _serializer);
			var cloudEntity2 = FatEntity.Convert<TimeSerieNoContract>(fatEntity, _serializer, null);
			var fatEntity2 = FatEntity.Convert(cloudEntity2, _serializer);

			Assert.IsNotNull(cloudEntity2);
			Assert.IsNotNull(fatEntity2);

			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity.PartitionKey);
			Assert.AreEqual(cloudEntity.RowKey, fatEntity.RowKey);


			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity2.PartitionKey);
			Assert.AreEqual(cloudEntity.RowKey, fatEntity2.RowKey);

			Assert.IsNotNull(cloudEntity2.Value);
			Assert.AreEqual(cloudEntity.Value.TimeValues.Length, cloudEntity2.Value.TimeValues.Length);

			for (int i = 0; i < timevalues.Length; i++)
			{
				Assert.AreEqual(cloudEntity.Value.TimeValues[i].Time, cloudEntity2.Value.TimeValues[i].Time);
				Assert.AreEqual(cloudEntity.Value.TimeValues[i].Value, cloudEntity2.Value.TimeValues[i].Value);
			}

			var data1 = fatEntity.GetData();
			var data2 = fatEntity2.GetData();
			Assert.AreEqual(data1.Length, data2.Length);
			for (int i = 0; i < data2.Length; i++)
			{
				Assert.AreEqual(data1[i], data2[i]);
			}
		}
	}
}
