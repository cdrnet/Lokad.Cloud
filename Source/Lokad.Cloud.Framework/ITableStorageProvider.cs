#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: this provider needs further documentation for all methods.

namespace Lokad.Cloud
{
	/// <summary>Abstraction for the Table Storage.</summary>
	/// <remarks>This provider represents a logical abstraction of the Table Storage,
	/// not the Table Storage itself. In particular, implementations handle paging
	/// and query splitting internally. Also, this provider implicitly relies on
	/// serialization to handle generic entities (not constrained by the few datatypes
	/// available to the Table Storage).</remarks>
	public interface ITableStorageProvider
	{
		// operations on tables

		bool CreateTable(string tableName);

		bool DeleteTable(string tableName);

		IEnumerable<string> GetTables();

		// operations on entities - get

		IEnumerable<CloudEntity<T>> Get<T>(string tableName);

		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey);

		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys);

		// operations on entities - insert

		void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

		// operations on entities - update

		void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

		// HACK: no 'upsert' (update or insert) available at the time
		// http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/4b902237-7cfb-4d48-941b-4802864fc274

		// operations on entities - update

		void Delete<T>(string tableName, string partitionKeys, IEnumerable<string> rowKeys);
	}
}
