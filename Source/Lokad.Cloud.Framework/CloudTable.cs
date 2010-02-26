﻿#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using Lokad.Quality;

namespace Lokad.Cloud
{
	/// <summary>Strong-typed utility wrapper for the <see cref="ITableStorageProvider"/>.</summary>
	/// <remarks>
	/// The purpose of the <c>CloudTable{T}</c> is to provide a strong-typed access to the
	/// table storage in the client code. Indeed, the row table storage provider typically
	/// let you (potentially) mix different types into a single table.
	/// </remarks>
	[Immutable]
	public class CloudTable<T>
	{
		readonly ITableStorageProvider _provider;
		readonly string _tableName;

		/// <summary>Name of the underlying table.</summary>
		public string Name
		{
			get { return _tableName; }
		}

		/// <remarks></remarks>
		public CloudTable(ITableStorageProvider provider, string tableName)
		{
			_provider = provider;
			_tableName = tableName;
		}

		/// <seealso cref="ITableStorageProvider.Get{T}(string, string)"/>
		public Maybe<CloudEntity<T>> Get(string partitionName, string rowKey)
		{
			return _provider.Get<T>(_tableName, partitionName, new[] { rowKey }).FirstOrEmpty();
		}

		/// <seealso cref="ITableStorageProvider.Get{T}(string)"/>
		public IEnumerable<CloudEntity<T>> Get()
		{
			return _provider.Get<T>(_tableName);
		}

		/// <seealso cref="ITableStorageProvider.Get{T}(string, string)"/>
		public IEnumerable<CloudEntity<T>> Get(string partitionKey)
		{
			return _provider.Get<T>(_tableName, partitionKey);
		}

		/// <seealso cref="ITableStorageProvider.Get{T}(string, string, string, string)"/>
		public IEnumerable<CloudEntity<T>> Get(string partitionKey, string startRowKey, string endRowKey)
		{
			return _provider.Get<T>(_tableName, partitionKey, startRowKey, endRowKey);
		}

		/// <seealso cref="ITableStorageProvider.Get{T}(string, string, IEnumerable{string})"/>
		public IEnumerable<CloudEntity<T>> Get(string partitionKey, IEnumerable<string> rowKeys)
		{
			return _provider.Get<T>(_tableName, partitionKey, rowKeys);
		}

		/// <seealso cref="ITableStorageProvider.Insert{T}(string, IEnumerable{CloudEntity{T}})"/>
		public void Insert(IEnumerable<CloudEntity<T>> entities)
		{
			_provider.Insert(_tableName, entities);
		}

		/// <seealso cref="ITableStorageProvider.Insert{T}(string, IEnumerable{CloudEntity{T}})"/>
		public void Insert(CloudEntity<T> entity)
		{
			_provider.Insert(_tableName, new []{entity});
		}

		/// <seealso cref="ITableStorageProvider.Update{T}(string, IEnumerable{CloudEntity{T}})"/>
		public void Update(IEnumerable<CloudEntity<T>> entities)
		{
			_provider.Update(_tableName, entities);
		}

		/// <seealso cref="ITableStorageProvider.Update{T}(string, IEnumerable{CloudEntity{T}})"/>
		public void Update(CloudEntity<T> entity)
		{
			_provider.Update(_tableName, new [] {entity});
		}

		/// <seealso cref="ITableStorageProvider.Upsert{T}(string, IEnumerable{CloudEntity{T}})"/>
		public void Upsert(IEnumerable<CloudEntity<T>> entities)
		{
			_provider.Upsert(_tableName, entities);
		}

		/// <seealso cref="ITableStorageProvider.Upsert{T}(string, IEnumerable{CloudEntity{T}})"/>
		public void Upsert(CloudEntity<T> entity)
		{
			_provider.Upsert(_tableName, new [] {entity});
		}

		/// <seealso cref="ITableStorageProvider.Delete{T}(string, string, IEnumerable{string})"/>
		public void Delete(string partitionKey, IEnumerable<string> rowKeys)
		{
			_provider.Delete<T>(_tableName, partitionKey, rowKeys);
		}

		/// <seealso cref="ITableStorageProvider.Delete{T}(string, string, IEnumerable{string})"/>
		public void Delete(string partitionKey, string rowKey)
		{
			_provider.Delete<T>(_tableName, partitionKey, new []{rowKey});
		}
	}
}
