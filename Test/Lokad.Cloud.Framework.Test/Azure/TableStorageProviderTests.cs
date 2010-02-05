#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections;
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
        readonly ITableStorageProvider Provider = GlobalSetup.Container.Resolve<ITableStorageProvider>();

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
        //TODO: #99.
        //Test the behavior of Get method (and overloads) with a non-existing table name.
        public void GetUnexistingTable()
        {
            const string notATableName = "IamNotATable";

            bool iSTestSuccess1 = false;
            try
            {
                var enumerable = Provider.Get<string>(notATableName);
                //Remove / Add the comment on the following line an the test will pass/fail.                
                int count = enumerable.Count();
            }
            catch (Exception exception)
            {
                iSTestSuccess1 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess1, "#C01");

            //Tests overloads
            #region
            bool iSTestSuccess2 = false;
            try
            {
                var enumerable = Provider.Get<string>(notATableName, "dummyPKey");
                //Remove / Add the comment on the following line an the test will pass/fail.                
                int count = enumerable.Count();
            }
            catch (Exception exception)
            {
                iSTestSuccess2 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess2, "#C02");


            bool iSTestSuccess3 = false;
            try
            {
                var enumerable = Provider.Get<string>(notATableName, "dummyPKey", new[] { "dummyRowKeyA", "dummyRowKeyB" });
                //Remove / Add the comment on the following line an the test will pass/fail.                  
                int count = enumerable.Count();
            }
            catch (Exception exception)
            {
                iSTestSuccess3 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess3, "#C03");

            bool iSTestSuccess4 = false;
            try
            {
                var enumerable = Provider.Get<string>(notATableName, "dummyPKey", "dummyRowKeyA", "dummyRowKeyB");
                //Remove / Add the comment on the following line an the test will pass/fail.                 
                int count = enumerable.Count();
            }
            catch (Exception exception)
            {
                iSTestSuccess4 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess4, "#C04");
            #endregion
        }

        [Test]
        //Test the behavior of Update, Insert and Delete methods with a non-existing table name.
        public void UpDateAndInsertUnexistingTable()
        {
            const string notATableName = "IamNotATable";

            //Insert.
            bool iSTestSuccess = false;
            try
            {
                Provider.Insert(notATableName, Entities(1, "dummyPKey", 10));
            }
            catch (Exception exception)
            {
                iSTestSuccess = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess, "#C05");

            //Update.
            bool iSTestSuccess2 = false;
            try
            {
                Provider.Update(notATableName, Entities(1, "dummyPKey", 10));
            }
            catch (Exception exception)
            {
                iSTestSuccess2 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess2, "#C06");

            //Delete.
            bool iSTestSuccess3 = false;
            try
            {
                Provider.Delete<string>(notATableName, "dummyPKey", new[] { "dummyRowKey" });
            }
            catch (Exception exception)
            {
                iSTestSuccess3 = (exception as InvalidOperationException) != null ? true : false;
            }
            Assert.IsTrue(iSTestSuccess3, "#C07");
        }

        [Test]
        public void GetUnexistingPartionName()
        {
            const string notAPartitionKey = "IAmNotAPartitionKey";

            var enumerable = Provider.Get<string>(TableName, notAPartitionKey);
            Assert.That(enumerable.Count() == 0, "#D01");

            var enumerable2 = Provider.Get<string>(TableName, notAPartitionKey, "dummyRowKeyA", "dummyRowKeyB");
            Assert.That(enumerable2.Count() == 0, "#D02");

            var enumerable3 = Provider.Get<string>(TableName, notAPartitionKey, new[] { "dummyRowKeyA", "dummyRowKeyB" });
            Assert.That(enumerable3.Count() == 0, "#D02");
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
        //TODO: #101
        public void IdempotenceOfDeleteMethod()
        {
            var Pkey = Guid.NewGuid().ToString();
            var RowKey = Guid.NewGuid().ToString();
            var entity = new CloudEntity<string> { PartitionKey = Pkey, RowRey = RowKey, Timestamp = DateTime.Now, Value = "value1" };

            //Insert entity.
            Provider.Insert(TableName, new[] { entity });
            Provider.Delete<string>(TableName, Pkey, new[] { RowKey });

            try
            {
                //Retry to delete.(same as deleting non existing entities).
                Provider.Delete<string>(TableName, Pkey, new[] { RowKey });
            }
            catch (Exception)
            {
                //Add/Remove comment will make the test pass/fail.
                //Assert.True(false,"#F01");
            }
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
