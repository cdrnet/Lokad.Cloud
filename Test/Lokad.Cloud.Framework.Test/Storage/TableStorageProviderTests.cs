﻿#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lokad.Cloud.Storage.Azure;
using Lokad.Cloud.Test;
using Lokad.Quality;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test
{
	[TestFixture]
	public class TableStorageProviderTests
	{
		readonly static Random Rand = new Random();

		// ReSharper disable InconsistentNaming
		readonly ITableStorageProvider Provider;
		// ReSharper restore InconsistentNaming

		const string TableName = "teststablestorageprovidermytable";

		[UsedImplicitly]
		public TableStorageProviderTests()
		{
			Provider = GlobalSetup.Container.Resolve<ITableStorageProvider>();
		}

		protected TableStorageProviderTests(ITableStorageProvider provider)
		{
			Provider = provider;
		}

		[TestFixtureSetUp]
		public void Setup()
		{
			Provider.CreateTable(TableName);
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			Provider.DeleteTable(TableName);
		}

		[Test]
		public void CreateDeleteTables()
		{
			var name = "n" + Guid.NewGuid().ToString("N");
			Assert.IsTrue(Provider.CreateTable(name), "#A01");
			Assert.IsFalse(Provider.CreateTable(name), "#A02");
			Assert.IsTrue(Provider.DeleteTable(name), "#A03");

			// replicating the test a 2nd time, to check for slow table deletion
			Assert.IsTrue(Provider.CreateTable(name), "#A04");
			Assert.IsTrue(Provider.DeleteTable(name), "#A05");

			Assert.IsFalse(Provider.DeleteTable(name), "#A06");

			var name2 = "IamNotATable";
			Assert.IsFalse(Provider.DeleteTable(name2), "#A07");
		}

		[Test]
		public void GetTables()
		{
			var tables = Provider.GetTables();
			Assert.IsTrue(tables.Contains(TableName), "#B07");
		}

		[Test]
		public void GetOnMissingTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");

			// checking the 4 overloads
			var enumerable = Provider.Get<string>(missingTableName);
			int count = enumerable.Count();
			Assert.AreEqual(0, count, "#A00");

			enumerable = Provider.Get<string>(missingTableName, "my-partition");
			count = enumerable.Count();
			Assert.AreEqual(0, count, "#A01");

			enumerable = Provider.Get<string>(missingTableName, "my-partition", "start", "end");
			count = enumerable.Count();
			Assert.AreEqual(0, count, "#A02");

			enumerable = Provider.Get<string>(missingTableName, "my-partition", new[] { "my-key" });
			count = enumerable.Count();
			Assert.AreEqual(0, count, "#A03");
		}

		[Test]
		public void GetOnJustDeletedTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");
			Assert.IsTrue(Provider.CreateTable(missingTableName), "#A01");
			Assert.IsTrue(Provider.DeleteTable(missingTableName), "#A02");

			// checking the 4 overloads
			var enumerable = Provider.Get<string>(missingTableName);
			int count = enumerable.Count();
			Assert.AreEqual(0, count, "#A00");

			enumerable = Provider.Get<string>(missingTableName, "my-partition");
			count = enumerable.Count();
			Assert.AreEqual(0, count, "#A01");

			enumerable = Provider.Get<string>(missingTableName, "my-partition", "start", "end");
			count = enumerable.Count();
			Assert.AreEqual(0, count, "#A02");

			enumerable = Provider.Get<string>(missingTableName, "my-partition", new[] { "my-key" });
			count = enumerable.Count();
			Assert.AreEqual(0, count, "#A03");
		}

		[Test]
		public void GetOnMissingPartitionShouldWork()
		{
			var missingPartition = Guid.NewGuid().ToString("N");

			var enumerable = Provider.Get<string>(TableName, missingPartition);
			Assert.That(enumerable.Count() == 0, "#D01");

			var enumerable2 = Provider.Get<string>(TableName, missingPartition, "dummyRowKeyA", "dummyRowKeyB");
			Assert.That(enumerable2.Count() == 0, "#D02");

			var enumerable3 = Provider.Get<string>(TableName, missingPartition, new[] { "dummyRowKeyA", "dummyRowKeyB" });
			Assert.That(enumerable3.Count() == 0, "#D02");
		}

		[Test]
		public void InsertOnMissingTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");
			Provider.Insert(missingTableName, Entities(1, "my-key", 10));

			// tentative clean-up
			Provider.DeleteTable(missingTableName);
		}

		[Test]
		public void InsertOnJustDeletedTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");
			Assert.IsTrue(Provider.CreateTable(missingTableName), "#A01");
			Assert.IsTrue(Provider.DeleteTable(missingTableName), "#A02");
			Provider.Insert(missingTableName, Entities(1, "my-key", 10));

			// tentative clean-up
			Provider.DeleteTable(missingTableName);
		}

		[Test]
		public void UpsertOnMissingTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");
			Provider.Upsert(missingTableName, Entities(1, "my-key", 10));

			// tentative clean-up
			Provider.DeleteTable(missingTableName);
		}

		[Test]
		public void UpsertShouldSupportLargeEntityCount()
		{
			var p1 = "00049999DatasetRepositoryTests";

			var entities = Entities(150, p1, 10);
			for (int i = 0; i < entities.Length; i++ )
			{
				entities[i].RowKey = "series+" + i;	
			}

			Provider.Upsert(TableName, entities);
			Provider.Upsert(TableName, entities); // idempotence

			var list = Provider.Get<string>(TableName, p1).ToArray();
			Assert.AreEqual(entities.Length, list.Length, "#A00");
		}

		[Test]
		public void UpsertShouldUpdateOrInsert()
		{
			var p1 = Guid.NewGuid().ToString("N");
			var p2 = Guid.NewGuid().ToString("N");

			var e1 = Entities(15, p1, 10);
			var e2 = Entities(25, p2, 10);
			var e1And2 = e1.Union(e2).ToArray();

			Provider.Upsert(TableName, e1);
			Provider.Upsert(TableName, e1And2);

			var count1 = Provider.Get<string>(TableName, p1).Count();
			Assert.AreEqual(e1.Length, count1, "#A00");

			var count2 = Provider.Get<string>(TableName, p2).Count();
			Assert.AreEqual(e2.Length, count2, "#A01");
		}

		[Test]
		public void InsertShouldHandleDistinctPartition()
		{
			var p1 = Guid.NewGuid().ToString("N");
			var p2 = Guid.NewGuid().ToString("N");

			var e1 = Entities(15, p1, 10);
			var e2 = Entities(25, p2, 10);
			var e1And2 = e1.Union(e2);

			Provider.Insert(TableName, e1And2);
		}

		[Test]
		public void GetWithPartitionShouldOnlySpecifiedPartition()
		{
			var p1 = Guid.NewGuid().ToString("N");
			var p2 = Guid.NewGuid().ToString("N");

			var e1 = Entities(15, p1, 10);
			var e2 = Entities(25, p2, 10);
			var e1And2 = e1.Union(e2);

			Provider.Insert(TableName, e1And2);

			var list1 = Provider.Get<string>(TableName, p1).ToArray();
			var count1 = list1.Length;
			Assert.AreEqual(e1.Length, count1, "#A00");
		}

		[Test]
		public void DeleteOnMissingTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");
			Provider.Delete<string>(missingTableName, "my-part", new[] { "my-key" });
		}

		[Test]
		public void DeleteOnJustDeletedTableShouldWork()
		{
			var missingTableName = "t" + Guid.NewGuid().ToString("N");
			Assert.IsTrue(Provider.CreateTable(missingTableName), "#A01");
			Assert.IsTrue(Provider.DeleteTable(missingTableName), "#A02");
			Provider.Delete<string>(missingTableName, "my-part", new[] { "my-key" });
		}

		[Test]
		public void DeleteOnMissingPartitionShouldWork()
		{
			var missingPartition = Guid.NewGuid().ToString("N");
			Provider.Delete<string>(TableName, missingPartition, new[] { "my-key" });
		}

		[Test]
		public void UpdateFailsOnMissingTable()
		{
			try
			{
				var missingTableName = "t" + Guid.NewGuid().ToString("N");
				Provider.Update(missingTableName, Entities(1, "my-key", 10));
				Assert.Fail("#A00");
			}
			catch (InvalidOperationException)
			{
			}
		}

		[Test]
		public void UpdateFailsOnJustDeletedTable()
		{
			try
			{
				var missingTableName = "t" + Guid.NewGuid().ToString("N");
				Assert.IsTrue(Provider.CreateTable(missingTableName), "#A01");
				Assert.IsTrue(Provider.DeleteTable(missingTableName), "#A02");
				Provider.Update(missingTableName, Entities(1, "my-key", 10));
				Assert.Fail("#A00");
			}
			catch (InvalidOperationException)
			{
			}
		}

		[Test]
		public void UpdateFailsOnMissingPartition()
		{
			try
			{
				var missingPartition = Guid.NewGuid().ToString("N");
				Provider.Update(TableName, Entities(1, missingPartition, 10));
				Assert.Fail("#A00");
			}
			catch (InvalidOperationException)
			{
			}
		}

		[Test]
		public void GetMethodStartEnd()
		{
			//This is a test on the ordered enumeration return by the GetMethod with StartRowKEy-EndRowKey.
			const int N = 250;
			string pKey = Guid.NewGuid().ToString();

			Provider.CreateTable(TableName);
			var entities = Enumerable.Range(0, N).Select(i => new CloudEntity<string>
					{
						PartitionKey = pKey,
						RowKey = "RowKey" + i,
						Value = Guid.NewGuid().ToString()
					});

			Provider.Insert(TableName, entities);

			var retrieved = Provider.Get<string>(TableName, pKey, null, null).ToArray();
			var retrievedSorted = retrieved.OrderBy(e => e.RowKey).ToArray();

			bool isOrdered = true;
			for (int i = 0; i < retrieved.Length; i++)
			{
				if (retrieved[i] != retrievedSorted[i])
				{
					isOrdered = false;
					break;
				}
			}
			Assert.That(isOrdered, "#C01");

			var retrieved2 = Provider.Get<string>(TableName, pKey, "RowKey25", null).ToArray();
			var retrievedSorted2 = retrieved2.OrderBy(e => e.RowKey).ToArray();

			bool isOrdered2 = true;
			for (int i = 0; i < retrieved2.Length; i++)
			{
				if (retrieved2[i] != retrievedSorted2[i])
				{
					isOrdered2 = false;
					break;
				}
			}
			Assert.That(isOrdered2, "#C02");

			var retrieved3 = Provider.Get<string>(TableName, pKey, null, "RowKey25").ToArray();
			var retrievedSorted3 = retrieved3.OrderBy(e => e.RowKey).ToArray();

			bool isOrdered3 = true;
			for (int i = 0; i < retrieved3.Length; i++)
			{
				if (retrieved3[i] != retrievedSorted3[i])
				{
					isOrdered3 = false;
					break;
				}
			}
			Assert.That(isOrdered3, "#C03");
		}

		[Test]
		public void InsertAndUpdateFailures()
		{
			var partitionKey = Guid.NewGuid().ToString();
			var rowKey = Guid.NewGuid().ToString();

			var entity = new CloudEntity<string>
				{
					PartitionKey = partitionKey,
					RowKey = rowKey,
					Timestamp = DateTime.UtcNow,
					Value = "value1"
				};

			// Insert entity.
			Provider.Insert(TableName, new[] { entity });

			// Insert Retry should fail.
			try
			{
				Provider.Insert(TableName, new[] { entity });
				Assert.Fail("#A01");
			}
			catch (InvalidOperationException)
			{
			}

			// Update entity twice should fail
			try
			{
				entity.Value = "value2";
				Provider.Update(TableName, new[] { entity, entity });
				Assert.Fail("#A02");
			}
			catch (InvalidOperationException)
			{
			}

			// Delete entity.
			Provider.Delete<string>(TableName, partitionKey, new[] { rowKey });

			// Update deleted entity should fail
			try
			{
				entity.Value = "value2";
				Provider.Update(TableName, new[] { entity });
				Assert.Fail("#A03");
			}
			catch (InvalidOperationException)
			{
			}

			// Insert entity twice should fail
			try
			{
				Provider.Insert(TableName, new[] { entity, entity });
				Assert.Fail("#A04");
			}
			catch (InvalidOperationException)
			{
			}
		}

		[Test]
		public void IdempotenceOfDeleteMethod()
		{
			var pkey = Guid.NewGuid().ToString("N");

			var entities = Range.Array(10).Convert(i =>
				new CloudEntity<string>
					{
						PartitionKey = pkey,
						RowKey = Guid.NewGuid().ToString("N"),
						Value = "nothing"
					});

			// Insert/delete entity.
			Provider.Insert(TableName, entities);

			// partial deletion
			Provider.Delete<string>(TableName, pkey, entities.Take(5).ToArray(e => e.RowKey));

			// complete deletion, but with overlap
			Provider.Delete<string>(TableName, pkey, entities.Convert(e => e.RowKey));

			// checking that all entities have been deleted
			var list = Provider.Get<string>(TableName, pkey, entities.Convert(e => e.RowKey));
			Assert.That(list.Count() == 0, "#A00");
		}

		[Test]
		public void CheckInsertHandlingOfEntityMaxCount()
		{
			var entityCount = 300; // above the max entity count limit
			var partitionKey = Guid.NewGuid().ToString();

			Provider.Insert(TableName, Entities(entityCount, partitionKey, 1));
			var retrievedCount = Provider.Get<string>(TableName, partitionKey).Count();

			Assert.AreEqual(entityCount, retrievedCount);
		}

		[Test]
		public void CheckRangeSelection()
		{
			var entityCount = 300; // above the max entity count limit
			var partitionKey = Guid.NewGuid().ToString();

			// entities are sorted
			var entities = Entities(entityCount, partitionKey, 1).OrderBy(e => e.RowKey).ToArray();

			Provider.Insert(TableName, entities);

			var retrievedCount = Provider.Get<string>(TableName, partitionKey,
				entities[150].RowKey, entities[200].RowKey).Count();

			// only the range should have been retrieved
			Assert.AreEqual(200 - 150, retrievedCount);
		}

		[Test]
		public void CheckInsertHandlingOfHeavyTransaction()
		{
			var entityCount = 50;
			var partitionKey = Guid.NewGuid().ToString();

			// 5 MB is above the max entity transaction payload
			Provider.Insert(TableName, Entities(entityCount, partitionKey, 100 * 1024));
			var retrievedCount = Provider.Get<string>(TableName, partitionKey).Count();

			Assert.AreEqual(entityCount, retrievedCount);
		}

		[Test]
		public void ErrorCodeExtraction()
		{
			// HACK: just reproducing the code being tested, no direct linking
			var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);

			var errorMessage =
@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<error xmlns=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"">
  <code>OperationTimedOut</code>
  <message xml:lang=""en-US"">Operation could not be completed within the specified time.
RequestId:f8e1e934-99ca-4a6f-bca7-e8e5fbd059ea
Time:2010-01-15T12:37:25.1611631Z</message>
</error>";

			var ex = new DataServiceRequestException("", new Exception(errorMessage));

			Assert.AreEqual("OperationTimedOut", AzurePolicies.GetErrorCode(ex));

		}

		[Test]
		public void EntityKeysShouldSupportSpecialCharacters()
		{
			// disallowed: /\#? must be <1 KB; we also disallow ' for simplicity
			var keys = new[] {"abc", "123", "abc-123", "abc def", "abc@def", "*", "+", "~%_;:.,"};

			var entities = keys.Select(key => new CloudEntity<string>
				{
					PartitionKey = key,
					RowKey = key,
					Value = key,
				}).ToArray();

			Provider.Insert(TableName, entities);

			var result = keys.Select(key => Provider.Get<string>(TableName, key, key).Value).ToArray();
			CollectionAssert.AreEqual(keys, result.Select(e => e.Value));
			CollectionAssert.AreEqual(keys, result.Select(e => e.PartitionKey));
			CollectionAssert.AreEqual(keys, result.Select(e => e.RowKey));

			foreach (var key in keys)
			{
				Provider.Delete<string>(TableName, key, new[] {key});
			}
		}

		[Test]
		public void EntitiesShouldHaveETagAfterInsert()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(3, partition, 10);

			Provider.Insert(TableName, entities);

			// note: ETags are not unique (they're actually the same per request)
			CollectionAssert.AllItemsAreNotNull(entities.Select(e => e.ETag));
		}

		[Test]
		public void EntitiesShouldHaveNewETagAfterUpdate()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(3, partition, 10);

			Provider.Insert(TableName, entities);

			var oldETags = entities.Select(e => e.ETag).ToArray();

			foreach(var entity in entities)
			{
				entity.Value += "modified";
			}

			Provider.Update(TableName, entities);

			var newETags = entities.Select(e => e.ETag).ToArray();
			CollectionAssert.AllItemsAreNotNull(newETags);
			CollectionAssert.AreNotEqual(newETags, oldETags);
		}

		[Test, ExpectedException(typeof(DataServiceRequestException))]
		public void UpdateOnRemotelyModifiedEntityShouldFailIfNotForced()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(1, partition, 10);
			var entity = entities.First();

			Provider.Insert(TableName, entities);

			entity.ETag = "abc";
			entity.Value = "def";

			Provider.Update(TableName, entities, false);
		}

		[Test]
		public void UpdateOnRemotelyModifiedEntityShouldNotFailIfForced()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(1, partition, 10);
			var entity = entities.First();

			Provider.Insert(TableName, entities);

			entity.ETag = "abc";
			entity.Value = "def";

			Provider.Update(TableName, entities, true);
		}

		[Test]
		public void UpdateOnRemotelyModifiedEntityShouldNotFailIfETagIsNull()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(1, partition, 10);
			var entity = entities.First();

			Provider.Insert(TableName, entities);

			entity.ETag = null;
			entity.Value = "def";

			Provider.Update(TableName, entities, false);
		}

		[Test, ExpectedException(typeof(DataServiceRequestException))]
		public void DeleteOnRemotelyModifiedEntityShouldFailIfNotForced()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(1, partition, 10);
			var entity = entities.First();

			Provider.Insert(TableName, entities);

			entity.ETag = "abc";
			entity.Value = "def";

			Provider.Delete(TableName, entities, false);
		}

		[Test]
		public void DeleteOnRemotelyModifiedEntityShouldNotFailIfForced()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(1, partition, 10);
			var entity = entities.First();

			Provider.Insert(TableName, entities);

			entity.ETag = "abc";
			entity.Value = "def";

			Provider.Delete(TableName, entities, true);
		}

		[Test]
		public void DeleteOnRemotelyModifiedEntityShouldNotFailIfETagIsNull()
		{
			var partition = Guid.NewGuid().ToString("N");
			var entities = Entities(1, partition, 10);
			var entity = entities.First();

			Provider.Insert(TableName, entities);

			entity.ETag = null;
			entity.Value = "def";

			Provider.Delete(TableName, entities, false);
		}

		CloudEntity<String>[] Entities(int count, string partitionKey, int entitySize)
		{
			return EntitiesInternal(count, partitionKey, entitySize).ToArray();
		}

		IEnumerable<CloudEntity<String>> EntitiesInternal(int count, string partitionKey, int entitySize)
		{
			for (int i = 0; i < count; i++)
			{
				yield return new CloudEntity<string>
					{
						PartitionKey = partitionKey,
						RowKey = Guid.NewGuid().ToString(),
						Value = RandomString(entitySize)
					};
			}
		}

		public static string RandomString(int size)
		{
			var builder = new StringBuilder();
			for (int i = 0; i < size; i++)
			{

				//26 letters in the alfabet, ascii + 65 for the capital letters
				builder.Append(Convert.ToChar(Convert.ToInt32(Rand.Next(26) + 65)));

			}
			return builder.ToString();
		}
	}
}
