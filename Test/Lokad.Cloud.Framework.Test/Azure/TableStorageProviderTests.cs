#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		//[Test]
		public void CheckInsertHandlingOfHeavyTransaction()
		{
			var entityCount = 50;
			var partitionKey = Guid.NewGuid().ToString();

			// 5 MB is above the max entity transaction payload
			Provider.Insert(TableName, Entities(entityCount, partitionKey, 100 * 1024));
			var retrievedCount = Provider.Get<string>(TableName, partitionKey).Count();

			Assert.AreEqual(entityCount, retrievedCount);
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
