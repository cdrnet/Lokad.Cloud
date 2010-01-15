#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

// TODO: #86 we need to better handle <code>OperationTimedOut</code> errors (may happen with heavy transaction).
// TODO: #87 missing retry policy implementation in this provider.

namespace Lokad.Cloud.Azure
{
	public class TableStorageProvider : ITableStorageProvider
	{
		// HACK: those tokens will probably be provided as constants in the StorageClient library
		const int MaxEntityTransactionCount = 100;
		const int MaxEntityTransactionPayload = 4*1024*1024; // 4 MB
		const string ContinuationNextRowKeyToken = "x-ms-continuation-NextRowKey";
		const string NextRowKeyToken = "NextRowKey";

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
			Enforce.That(() => tableName);

			var context = _tableStorage.GetDataServiceContext();
			context.MergeOption = MergeOption.NoTracking;

			string continuationRowKey = null;

			do
			{
				var query = context.CreateQuery<FatEntity>(tableName);

				if (null != continuationRowKey)
				{
					query = query.AddQueryOption(NextRowKeyToken, continuationRowKey);
				}

				var response = query.Execute() as QueryOperationResponse;
				foreach (FatEntity fatEntity in response)
				{
					yield return FatEntity.Convert<T>(fatEntity, _formatter);
				}

				if (response.Headers.ContainsKey(ContinuationNextRowKeyToken))
				{
					continuationRowKey = response.Headers[ContinuationNextRowKeyToken];
				}
				else
				{
					continuationRowKey = null;
				}

			} while (null != continuationRowKey);
		}

		public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey)
		{
			Enforce.That(() => tableName);
			Enforce.That(() => partitionKey);
			Enforce.That(!partitionKey.Contains("'"), "Incorrect char in partitionKey.");

			var context = _tableStorage.GetDataServiceContext();
			context.MergeOption = MergeOption.NoTracking;

			string continuationRowKey = null;

			do
			{
				var query = context.CreateQuery<FatEntity>(tableName)
					.AddQueryOption("$filter", string.Format("PartitionKey eq '{0}'", partitionKey));

				if (null != continuationRowKey)
				{
					query = query.AddQueryOption(NextRowKeyToken, continuationRowKey);
				}

				var response = query.Execute() as QueryOperationResponse;
				foreach (FatEntity fatEntity in response)
				{
					yield return FatEntity.Convert<T>(fatEntity, _formatter);
				}

				if (response.Headers.ContainsKey(ContinuationNextRowKeyToken))
				{
					continuationRowKey = response.Headers[ContinuationNextRowKeyToken];
				}
				else
				{
					continuationRowKey = null;
				}

			} while (null != continuationRowKey);
		}

		public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
		{
			Enforce.That(() => tableName);
			Enforce.That(() => partitionKey);
			Enforce.That(!partitionKey.Contains("'"), "Incorrect char in partitionKey.");

			var context = _tableStorage.GetDataServiceContext();
			context.MergeOption = MergeOption.NoTracking;

			foreach(var slice in rowKeys.Slice(MaxEntityTransactionCount))
			{
				// work-around the limitation of ADO.NET that does not provide a native way
				// of query a set of specified entities directly.
				var builder = new StringBuilder();
				builder.Append(string.Format("(PartitionKey eq '{0}') and (", partitionKey));
				for (int i = 0; i < slice.Length; i++)
				{
					// in order to avoid SQL-injection-like problems 
					Enforce.That(!slice[i].Contains("'"), "Incorrect char in rowKey.");

					builder.Append(string.Format("(RowKey eq '{0}')", slice[i]));
					if (i < slice.Length - 1)
					{
						builder.Append(" or ");
					}
				}
				builder.Append(")");

				string continuationRowKey = null;

				do
				{
					var query = context.CreateQuery<FatEntity>(tableName)
						.AddQueryOption("$filter", builder.ToString());

					if(null != continuationRowKey)
					{
						query = query.AddQueryOption(NextRowKeyToken, continuationRowKey);
					}

					var response = query.Execute() as QueryOperationResponse;
					foreach (FatEntity fatEntity in response)
					{
						yield return FatEntity.Convert<T>(fatEntity, _formatter);
					}

					if (response.Headers.ContainsKey(ContinuationNextRowKeyToken))
					{
						continuationRowKey = response.Headers[ContinuationNextRowKeyToken];
					}
					else
					{
						continuationRowKey = null;
					}

				} while (null != continuationRowKey);
			}
		}

		public void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			var context = _tableStorage.GetDataServiceContext();
			context.MergeOption = MergeOption.NoTracking;

			var fatEntities = entities.Select(e => FatEntity.Convert(e, _formatter));

			foreach (var slice in SliceEntities(fatEntities))
			{
				foreach (var fatEntity in slice)
				{
					context.AddObject(tableName, fatEntity);
				}

				// TODO: bug, should be replaced by 'SaveChangesWithRetries' once code is validated
				context.SaveChanges(SaveChangesOptions.Batch);
			}
		}

		public void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			var context = _tableStorage.GetDataServiceContext();

			var fatEntities = entities.Select(e => FatEntity.Convert(e, _formatter));

			foreach (var slice in SliceEntities(fatEntities))
			{
				foreach (var fatEntity in slice)
				{
					// entities should be updated in a single round-trip
					context.AttachTo(tableName, fatEntity, "*");
					context.UpdateObject(fatEntity);
				}

				// TODO: bug, should be replaced by 'SaveChangesWithRetries' once code is validated
				context.SaveChanges(SaveChangesOptions.Batch);
			}
		}

		/// <summary>Slice entities according the payload limitation of
		/// the transaction group, plus the maximal number of entities to
		/// be embedded into a single transaction.</summary>
		static IEnumerable<FatEntity[]> SliceEntities(IEnumerable<FatEntity> entities)
		{
			var accumulator = new List<FatEntity>(100);
			var payload = 0;
			foreach(var entity in entities)
			{
				var entityPayLoad = entity.GetPayload();

				if(accumulator.Count >= MaxEntityTransactionCount || 
					payload + entityPayLoad >= MaxEntityTransactionPayload)
				{
					yield return accumulator.ToArray();
					accumulator.Clear();
				}

				accumulator.Add(entity);
				payload += entityPayLoad;
			}

			if(accumulator.Count > 0)
			{
				yield return accumulator.ToArray();
			}
		}

		public void Delete<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
		{
			var context = _tableStorage.GetDataServiceContext();

			foreach (var slice in rowKeys.Slice(MaxEntityTransactionCount))
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

				// TODO: bug, should be replaced by 'SaveChangesWithRetries' once code is validated
				context.SaveChanges(SaveChangesOptions.Batch);
			}
		}
	}
}
