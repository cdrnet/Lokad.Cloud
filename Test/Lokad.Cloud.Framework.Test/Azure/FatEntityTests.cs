#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace Lokad.Cloud.Azure.Test
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

		readonly IBinaryFormatter Formatter = GlobalSetup.Container.Resolve<IBinaryFormatter>();

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
					RowRey = "key",
					Value = serie
				};

			var fatEntity = FatEntity.Convert(cloudEntity, Formatter);
			var cloudEntity2 = FatEntity.Convert<TimeSerie>(fatEntity, Formatter);
			var fatEntity2 = FatEntity.Convert(cloudEntity2, Formatter);

			Assert.IsNotNull(cloudEntity2);
			Assert.IsNotNull(fatEntity2);

			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity.PartitionKey);
			Assert.AreEqual(cloudEntity.RowRey, fatEntity.RowKey);
			

			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity2.PartitionKey);
			Assert.AreEqual(cloudEntity.RowRey, fatEntity2.RowKey);

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
				RowRey = "key",
				Value = serie
			};

			var fatEntity = FatEntity.Convert(cloudEntity, Formatter);
			var cloudEntity2 = FatEntity.Convert<TimeSerieNoContract>(fatEntity, Formatter);
			var fatEntity2 = FatEntity.Convert(cloudEntity2, Formatter);

			Assert.IsNotNull(cloudEntity2);
			Assert.IsNotNull(fatEntity2);

			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity.PartitionKey);
			Assert.AreEqual(cloudEntity.RowRey, fatEntity.RowKey);


			Assert.AreEqual(cloudEntity.PartitionKey, fatEntity2.PartitionKey);
			Assert.AreEqual(cloudEntity.RowRey, fatEntity2.RowKey);

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
