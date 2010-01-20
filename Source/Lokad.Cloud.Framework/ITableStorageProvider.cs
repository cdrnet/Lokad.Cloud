#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;

// TODO: #89 missing 'UpdateOrInsert' method

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
		/// <summary>Creates a new table if it does not exist already.</summary>
		/// <returns><c>true</c> if a new table has been created.</returns>
		bool CreateTable(string tableName);

		/// <summary>Deletes a table if it exists.</summary>
		/// <returns><c>true</c> if the table has been deleted.</returns>
		bool DeleteTable(string tableName);

		/// <summary>Returns the list of all the tables that exist in the storage.</summary>
		IEnumerable<string> GetTables();

		/// <summary>Iterates through all entities of a given table.</summary>
		/// <remarks>The enumeration is typically expected to be lazy, iterating through
		/// all the entities with paged request.</remarks>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName);

		/// <summary>Iterates through all entities of a given table and partition.</summary>
		/// <remarks>The enumeration is typically expected to be lazy, iterating through
		/// all the entities with paged request.</remarks>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey);

		/// <summary>Iterates through a range of entities of a given table and partition.</summary>
		/// <param name="tableName">Name of the Table.</param>
		/// <param name="partitionKey">Name of the partition.</param>
		/// <param name="startRowKey">Inclusive start row key. If <c>null</c>, no start range
		/// constraint is enforced.</param>
		/// <param name="endRowKey">Exclusive end row key. If <c>null</c>, no ending range
		/// constraint is enforced.</param>
		/// <remarks>The enumeration is typically expected to be lazy, iterating through
		/// all the entities with paged request.</remarks>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey);

		/// <summary>Iterates through all entities specified by their row keys.</summary>
		/// <remarks>The enumeration is typically expected to be lazy, iterating through
		/// all the entities with paged request.</remarks>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys);

		/// <summary>Inserts a collection of new entities into the table storage.</summary>
		/// <remarks>
		/// <para>The call is expected to fail on the first encountered already-existing
		/// entity. Results are not garanteed if one or several entities already exist.
		/// </para>
		/// <para>There is no upper limit on the number of entities provided through
		/// the enumeration. The implementations are expected to lazily iterates
		/// and to create batch requests as the move forward.
		/// </para>
		/// </remarks>
		void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

		/// <summary>Updates a collection of existing entities into the table storage.</summary>
		/// <remarks>
		/// <para>The call is expected to fail on the first non-existing entity. 
		/// Results are not garanteed if one or several entities do not exist already.
		/// </para>
		/// <para>There is no upper limit on the number of entities provided through
		/// the enumeration. The implementations are expected to lazily iterates
		/// and to create batch requests as the move forward.
		/// </para>
		/// </remarks>
		void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

		// HACK: no 'upsert' (update or insert) available at the time
		// http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/4b902237-7cfb-4d48-941b-4802864fc274

		/// <summary>Deletes all specified entities.</summary>
		/// <remarks>The implementation is expected to lazily iterate through all row keys
		/// and send batch deletion request to the underlying storage.</remarks>
		void Delete<T>(string tableName, string partitionKeys, IEnumerable<string> rowKeys);
	}
}
