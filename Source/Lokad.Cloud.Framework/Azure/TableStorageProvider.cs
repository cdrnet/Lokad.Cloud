#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

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
		readonly ActionPolicy _storagePolicy;

		public TableStorageProvider(CloudTableClient tableStorage, IBinaryFormatter formatter)
		{
			_tableStorage = tableStorage;
			_formatter = formatter;
			_storagePolicy = AzurePolicies.TransientTableErrorBackOff;
		}

		public bool CreateTable(string tableName)
		{
			var flag = false;
			AzurePolicies.SlowInstantiation.Do(() => 
				flag =  _tableStorage.CreateTableIfNotExist(tableName));

			return flag;
		}

		public bool DeleteTable(string tableName)
		{
			var flag = false;
			AzurePolicies.SlowInstantiation.Do(() => 
				flag = _tableStorage.DeleteTableIfExist(tableName));

			return flag;
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

				QueryOperationResponse response = null;
				FatEntity[] fatEntities = null;

				_storagePolicy.Do(() =>
					{
						response = query.Execute() as QueryOperationResponse;
						fatEntities = ((IEnumerable<FatEntity>) response).ToArray();
					});

				foreach (FatEntity fatEntity in fatEntities)
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

				QueryOperationResponse response = null;
				FatEntity[] fatEntities = null;

				_storagePolicy.Do(() =>
				{
					response = query.Execute() as QueryOperationResponse;
					fatEntities = ((IEnumerable<FatEntity>)response).ToArray();
				});

				foreach (var fatEntity in fatEntities)
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

					QueryOperationResponse response = null;
					FatEntity[] fatEntities = null;

					_storagePolicy.Do(() =>
					{
						response = query.Execute() as QueryOperationResponse;
						fatEntities = ((IEnumerable<FatEntity>)response).ToArray();
					});

					foreach (FatEntity fatEntity in fatEntities)
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

				_storagePolicy.Do(() =>
					{
						try
						{
							context.SaveChanges(SaveChangesOptions.Batch);
						}
						catch (DataServiceRequestException ex)
						{
							var errorCode = AzurePolicies.GetErrorCode(ex);

							if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
							{
								// if batch does not work, then split into elementary requests
								// PERF: it would be better to split the request in two and retry
								context.SaveChanges();
							}
							else
							{
								throw;
							}
						}
					});
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

				_storagePolicy.Do(() =>
				{
					try
					{
						context.SaveChanges(SaveChangesOptions.Batch);
					}
					catch (DataServiceRequestException ex)
					{
						var errorCode = AzurePolicies.GetErrorCode(ex);
						
						if (errorCode == StorageErrorCodeStrings.OperationTimedOut)
						{
							// if batch does not work, then split into elementary requests
							// PERF: it would be better to split the request in two and retry
							context.SaveChanges();
						}
						else
						{
							throw;
						}
					}
				});
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

				_storagePolicy.Do(() => 
					context.SaveChanges(SaveChangesOptions.Batch));
			}	
		}

		
	}
}
