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
			Assert.IsTrue(Provider.CreateTable(name));
			Assert.IsTrue(Provider.DeleteTable(name));

			// replicating the test a 2nd time, to check for slow table deletion
			Assert.IsTrue(Provider.CreateTable(name));
			Assert.IsTrue(Provider.DeleteTable(name));

			Assert.IsFalse(Provider.DeleteTable(name));
		}

		[Test]
		public void GetTables()
		{
			var tables = Provider.GetTables();
			Assert.IsTrue(tables.Contains(TableName));
		}

		[Test]
		public void CheckInsertHandlingOfEntityMaxCount() 
		{
			var entityCount = 300; // above the max entity count limit
			var partitionKey = Guid.NewGuid().ToString();

			Provider.Insert(TableName, Entities(entityCount, partitionKey, 1));
			var retrievedCount = Provider.Get<string>(TableName,partitionKey).Count();

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
