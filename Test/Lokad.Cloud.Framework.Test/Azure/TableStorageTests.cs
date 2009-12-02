#region (c)2009 Lokad - New BSD license

// Copyright (c) Lokad 2009 
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Azure.Test
{
	[TestFixture]
	public class TableStorageTests
	{
		Random _random = new Random();

		public void RoundTrip()
		{
			// Tables take a little while to be deleted, need to change name
			string TableName = "TestTable" + _random.Next().ToString();

			TableStorage storage = GlobalSetup.Container.Resolve<TableStorage>();
			Assert.IsNotNull(storage);

			storage.Client.CreateTable(TableName);

			MyItem item1 = new MyItem("01", 1, new byte[100]);
			_random.NextBytes(item1.FileContent);

			MyItem item2 = new MyItem("02", 10, new byte[130]);
			_random.NextBytes(item2.FileContent);

			MyItem item3 = new MyItem("03", 12, new byte[110]);
			_random.NextBytes(item3.FileContent);

			storage.Context.AddObject(TableName, item1);
			storage.Context.AddObject(TableName, item2);
			storage.Context.AddObject(TableName, item3);
			storage.Context.SaveChanges();

			var output =
				(from i in storage.Context.CreateQuery<MyItem>(TableName)
				 where i.RowKey == "01"
				 select i).ToList();
			Assert.AreEqual(1, output.Count);
			CollectionAssert.AreEquivalent(item1.FileContent, output[0].FileContent);

			output =
				(from i in storage.Context.CreateQuery<MyItem>(TableName)
				 where i.Count >= 10
				 select i).ToList();
			Assert.AreEqual(2, output.Count);

			storage.Context.DeleteObject(output[0]);
			storage.Context.SaveChanges();

			output =
				(from i in storage.Context.CreateQuery<MyItem>(TableName)
				 where i.Count >= 10
				 select i).ToList();
			Assert.AreEqual(1, output.Count);

			storage.Client.DeleteTable(TableName);
		}

		internal class MyItem : TableServiceEntity
		{
			public MyItem(string key, int count, byte[] content)
				: base("Items", key)
			{
				Count = count;
				FileContent = content;
			}

			public int Count { get; set; }

			public byte[] FileContent { get; set; }

		}
	}
}
