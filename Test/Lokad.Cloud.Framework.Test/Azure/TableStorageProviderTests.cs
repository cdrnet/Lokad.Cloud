#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Lokad.Cloud.Azure.Test
{
    [TestFixture]
    public class TableStorageProviderTests
    {
        readonly static Random Rand = new Random();

// ReSharper disable InconsistentNaming
        readonly ITableStorageProvider Provider = GlobalSetup.Container.Resolve<ITableStorageProvider>();
// ReSharper restore InconsistentNaming

        const string TableName = "teststablestorageprovidermytable";

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
			var missingTableName = Guid.NewGuid().ToString("N");

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

			enumerable = Provider.Get<string>(missingTableName, "my-partition", new [] { "my-key"});
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
			var missingTableName = Guid.NewGuid().ToString("N");
			Provider.Insert(missingTableName, Entities(1, "my-key", 10));

			// tentative clean-up
			Provider.DeleteTable(missingTableName);
		}

		[Test]
		public void DeleteOnMissingTableShouldWork()
		{
			var missingTableName = Guid.NewGuid().ToString("N");
			Provider.Delete<string>(missingTableName, "my-part", new []{"my-key"});
		}

		[Test]
		public void DeleteOnMissingPartitionShouldWork()
		{
			var missingPartition = Guid.NewGuid().ToString("N");
			Provider.Delete<string>(TableName, missingPartition, new[] { "my-key" });
		}

        [Test]
        public void GetMethodStartEnd()
        {
            //This is a test on the ordered enumeration return by the GetMethod with StartRowKEy-EndRowKey.
            const int N = 250;
            string pKey = Guid.NewGuid().ToString();

            Provider.CreateTable(TableName);
            var entities = Enumerable.Range(0,N).Select(i=> new CloudEntity<string>
        			{
        				PartitionKey = pKey,
						RowRey = "RowKey" +i,
						Value = Guid.NewGuid().ToString()
        			});

            Provider.Insert(TableName, entities);

            var retrieved = Provider.Get<string>(TableName, pKey, null, null).ToArray();
            var retrievedSorted = retrieved.OrderBy(e => e.RowRey).ToArray();

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
            var retrievedSorted2 = retrieved2.OrderBy(e => e.RowRey).ToArray();

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
            var retrievedSorted3 = retrieved3.OrderBy(e => e.RowRey).ToArray();

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
            var Pkey = Guid.NewGuid().ToString();
            var RowKey = Guid.NewGuid().ToString();

            var entity = new CloudEntity<string> { PartitionKey = Pkey, RowRey = RowKey, Timestamp = DateTime.Now, Value = "value1" };

            //Insert entity.
            Provider.Insert(TableName, new[] { entity });

            bool iSTestSuccess1 = false;
            try
            {
                //retry should fail.
                Provider.Insert(TableName, new[] { entity });
            }
            catch (Exception exception)
            {
                iSTestSuccess1 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.That(iSTestSuccess1, "#E01");

            bool isTestSuccess2 = false;
            //delete the entity.
            Provider.Delete<string>(TableName, Pkey, new[] { RowKey });
            try
            {
                entity.Value = "value2";
                Provider.Update(TableName, new[] { entity });
            }
            catch (Exception exception)
            {
                //Update should fail.
                isTestSuccess2 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.That(isTestSuccess2, "#E02");
        }

        [Test]
        public void IdempotenceOfDeleteMethod()
        {
            var pkey = Guid.NewGuid().ToString("N");

        	var entities = Range.Array(10).Convert(i =>
        		new CloudEntity<string>
        			{
        				PartitionKey = pkey,
						RowRey = Guid.NewGuid().ToString("N"),
						Value = "nothing"
        			});

            // Insert/delete entity.
            Provider.Insert(TableName, entities);

			// partial deletion
            Provider.Delete<string>(TableName, pkey, entities.Take(5).ToArray(e => e.RowRey));

			// complete deletion, but with overlap
			Provider.Delete<string>(TableName, pkey, entities.Convert(e => e.RowRey));

			// checking that all entities have been deleted
        	var list = Provider.Get<string>(TableName, pkey, entities.Convert(e => e.RowRey));
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
            var entities = Entities(entityCount, partitionKey, 1).OrderBy(e => e.RowRey).ToArray();

            Provider.Insert(TableName, entities);

            var retrievedCount = Provider.Get<string>(TableName, partitionKey,
                entities[150].RowRey, entities[200].RowRey).Count();

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

        IEnumerable<CloudEntity<String>> Entities(int count, string partitionKey, int entitySize)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new CloudEntity<string>
                    {
                        PartitionKey = partitionKey,
                        RowRey = Guid.NewGuid().ToString(),
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
