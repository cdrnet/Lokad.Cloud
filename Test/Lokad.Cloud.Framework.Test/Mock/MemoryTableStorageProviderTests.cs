#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Storage.InMemory;
using Lokad.Cloud.Storage.Tables;
using Lokad.Cloud.Storage.Test;
using NUnit.Framework;

namespace Lokad.Cloud.Mock.Test
{
	/// <remarks>Includes all unit tests for the real table provider</remarks>
	[TestFixture]
	public class MemoryTableStorageProviderTests : TableStorageProviderTests
	{
		public MemoryTableStorageProviderTests()
			: base(new MemoryTableStorageProvider())
		{
		}

		[Test]
		public void CreateAndGetTable()
		{
			//Single thread.
			var tableStorage = new MemoryTableStorageProvider();
			for (int i = 0; i <= 5; i++)
			{
				tableStorage.CreateTable(i.ToString());
			}
			var retrievedTables = tableStorage.GetTables();
			Assert.AreEqual(6, retrievedTables.Count(),"#A01");

			//Remove tables.
			Assert.False(tableStorage.DeleteTable("Table_that_does_not_exist"), "#A02");
			var isSuccess = tableStorage.DeleteTable(4.ToString());
			retrievedTables = tableStorage.GetTables();
			Assert.IsTrue(isSuccess, "#A03");
			Assert.AreEqual(5, retrievedTables.Count(), "#A04");

		}

		[Test]
		public void InsertAndGetMethodSingleThread()
		{
			var tableStorage = new MemoryTableStorageProvider();
			tableStorage.CreateTable("myTable");

			const int partitionCount = 10;

			//Creating entities: a hundred. Pkey created with the last digit of a number between 0 and 99.
			var entities =
				Enumerable.Range(0, 100).Select(
					i =>
						new CloudEntity<object>()
							{
								PartitionKey = "Pkey-" + (i % partitionCount).ToString("0"),
								RowKey = "RowKey-" + i.ToString("00"),
								Value = new object()
							}
					);

			//Insert entities.
			tableStorage.Insert("myTable", entities);

			//retrieve all of them.
			var retrievedEntities1 = tableStorage.Get<object>("myTable");
			Assert.AreEqual(100, retrievedEntities1.Count(),"#B01");

			//Test overloads...
			var retrievedEntites2 = tableStorage.Get<object>("myTable", "Pkey-9");
			Assert.AreEqual(10, retrievedEntites2.Count(), "#B02");

			var retrievedEntities3 = tableStorage.Get<object>("myTable", "Pkey-7",
				new[] { "RowKey-27", "RowKey-37", "IAmNotAKey" });

			Assert.AreEqual(2, retrievedEntities3.Count(), "#B03");

			//The following tests handle the exclusive and inclusive bounds of key search.
			var retrieved4 = tableStorage.Get<object>("myTable", "Pkey-1", "RowKey-01", "RowKey-91");
			Assert.AreEqual(9, retrieved4.Count(), "#B04");

			var retrieved5 = tableStorage.Get<object>("myTable", "Pkey-1", "RowKey-01", null);
			Assert.AreEqual(10, retrieved5.Count(), "#B05");

			var retrieved6 = tableStorage.Get<object>("myTable", "Pkey-1", null, null);
			Assert.AreEqual(10, retrieved6.Count(), "#B06");

			var retrieved7 = tableStorage.Get<object>("myTable", "Pkey-1", null, "RowKey-21");
			Assert.AreEqual(2, retrieved7.Count(), "#B07");

			//The next test should handle non existing table names.
			var emptyEnumeration =  tableStorage.Get<object>("IAmNotATable", "IaMNotAPartiTion");
			Assert.AreEqual(0, emptyEnumeration.Count(), "#B08");
		}

		[Test]
		public void InsertUpdateAndDeleteSingleThread()
		{
			var tableStorage = new MemoryTableStorageProvider();
			tableStorage.CreateTable("myTable");

			int partitionCount = 10;

			var entities =
				Enumerable.Range(0, 100).Select(
					i =>
						new CloudEntity<object>
							{
							PartitionKey = "Pkey-" + (i % partitionCount).ToString("0"),
							RowKey = "RowKey-" + i.ToString("00"),
							Value = new object()
						}
					);
			tableStorage.Insert("myTable", entities);

			var isSucces = false;
			try
			{
				tableStorage.Insert("myTable", new[] { new CloudEntity<object>() { PartitionKey = "Pkey-6", RowKey = "RowKey-56" } });
			}
			catch (Exception exception)
			{
				isSucces = (exception as InvalidOperationException) == null ? false : true;
			}
			Assert.IsTrue(isSucces);

			tableStorage.CreateTable("myNewTable");
			tableStorage.Insert("myNewTable",
				new[] { new CloudEntity<object> { PartitionKey = "Pkey-6", RowKey = "RowKey-56", Value = new object() } });

			Assert.AreEqual(2, tableStorage.GetTables().Count());

			tableStorage.Update("myNewTable", new[] { new CloudEntity<object> { PartitionKey = "Pkey-6", RowKey = "RowKey-56", Value = 2000 } }, true);
			Assert.AreEqual(2000, (int)tableStorage.Get<object>("myNewTable", "Pkey-6", new[] { "RowKey-56" }).First().Value);

			tableStorage.Delete<object>("myNewTable", "Pkey-6", new[] { "RowKey-56" });

			var retrieved = tableStorage.Get<object>("myNewTable");
			Assert.AreEqual(0, retrieved.Count());

		}

		[Test]
		public void CreateAndGetTablesMultiThread()
		{
			//Multi thread.
			const int M = 32;
			var tableStorage = new MemoryTableStorageProvider();

			var threads = Enumerable.Range(0, M).Select(i => new Thread(CreateTables)).ToArray();
			var threadsParameters =
				Enumerable.Range(0, M).Select(
					i => new ThreadParameter { TableStorage = tableStorage, ThreadId = "treadId" + i.ToString() }).
					ToArray();

			for (int i = 0; i < M; i++)
			{
				threads[i].Start(threadsParameters[i]);
			}
			Thread.Sleep(2000);

			Assert.AreEqual(10, tableStorage.GetTables().Distinct().Count());

		}

		static void CreateTables(object parameter)
		{
			if (parameter is ThreadParameter)
			{
				var castedParameters = (ThreadParameter)parameter;
				for (int i = 0; i < 10; i++)
				{
					castedParameters.TableStorage.CreateTable(i.ToString());
				}
			}
		}

		class ThreadParameter
		{
			public MemoryTableStorageProvider TableStorage { get; set; }
			public string ThreadId { get; set; }
		}
	}
}
