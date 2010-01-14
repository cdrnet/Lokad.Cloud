using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Azure
{
	public class TableStorageProvider : ITableStorageProvider
	{
		const int MaxEntityTranscationCount = 100;

		readonly CloudTableClient _tableStorage;
		readonly IBinaryFormatter _formatter;

		public TableStorageProvider(CloudTableClient tableStorage, IBinaryFormatter formatter)
		{
			_tableStorage = tableStorage;
			_formatter = formatter;
		}

		public bool CreateTable(string tableName)
		{
			return _tableStorage.CreateTableIfNotExist(tableName);
		}

		public bool DeleteTable(string tableName)
		{
			return _tableStorage.DeleteTableIfExist(tableName);
		}

		public IEnumerable<string> GetTables()
		{
			return _tableStorage.ListTables();
		}

		public IEnumerable<CloudEntity<T>> Get<T>(string tableName)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
		{
			var context = _tableStorage.GetDataServiceContext();
			context.MergeOption = MergeOption.NoTracking;

			foreach(var slice in rowKeys.Slice(MaxEntityTranscationCount))
			{
				// work-around the limitation of ADO.NET that does not provide a native way
				// of query a set of specified entities directly.
				var builder = new StringBuilder();
				builder.Append(string.Format("(PartitionKey eq '{0}') and (", partitionKey));
				for (int i = 0; i < slice.Length; i++)
				{
					// in order to avoid SQL-injection-like problems 
					Enforce.That(!slice[i].Contains("'"), "Incorrect rowKey");

					builder.Append(string.Format("(RowKey eq '{0}')", slice[i]));
					if (i < slice.Length - 1)
					{
						builder.Append(" or ");
					}
				}
				builder.Append(")");

				var query = context.CreateQuery<FatEntity>(tableName)
					.AddQueryOption("$filter", builder.ToString());

				foreach(var fatEntity in query.Execute())
				{
					// TODO: need to be handling continuous tokens here

					var stream = new MemoryStream(fatEntity.GetData()) {Position = 0};
					var val = (T)_formatter.Deserialize(stream, typeof (T));

					yield return new CloudEntity<T>
						{
							PartitionKey = fatEntity.PartitionKey,
							RowRey = fatEntity.RowKey,
							Timestamp = fatEntity.Timestamp,
							Value = val
						};
				}
			}
		}

		public void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			throw new NotImplementedException();
		}

		public void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			throw new NotImplementedException();
		}

		public void Delete<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
		{
			var context = _tableStorage.GetDataServiceContext();

			foreach (var slice in rowKeys.Slice(MaxEntityTranscationCount))
			{
				foreach (var rowKey in slice)
				{
					// Deleting entities in 1 roundtrip
					// http://blog.smarx.com/posts/deleting-entities-from-windows-azure-without-querying-first
					var mock = new FatEntity
						{
							PartitionKey = partitionKey,
							RowKey = rowKey
						};

					context.AttachTo(tableName, mock, "*");
					context.DeleteObject(mock);
				}

				// TODO: should be replaced by 'SaveChangesWithRetries' once code is validated
				context.SaveChanges(SaveChangesOptions.Batch);
			}
		}
	}
}
