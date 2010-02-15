#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Data.Services.Client;

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
		/// <returns><c>true</c> if a new table has been created.
        /// <c>false</c> if the table already exists.
		/// </returns>
		bool CreateTable(string tableName);

		/// <summary>Deletes a table if it exists.</summary>
		/// <returns><c>true</c> if the table has been deleted.
		/// <c>false</c> if the table does not exist.
		/// </returns>
		bool DeleteTable(string tableName);

		/// <summary>Returns the list of all the tables that exist in the storage.</summary>
		IEnumerable<string> GetTables();

		/// <summary>Iterates through all entities of a given table.</summary>
		/// <remarks>The enumeration is typically expected to be lazy, iterating through
		/// all the entities with paged request.</remarks>
        ///<exception cref="InvalidOperationException"> thrown if the table does not exist.</exception>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName);

		/// <summary>Iterates through all entities of a given table and partition.</summary>
        /// <remarks><para>The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request.</para>
        /// <para>If the partition key does not exists the collection is empty.</para>
        /// </remarks>
        ///<exception cref="InvalidOperationException"> thrown if the table does not exist.</exception>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey);

		/// <summary>Iterates through a range of entities of a given table and partition.</summary>
		/// <param name="tableName">Name of the Table.</param>
		/// <param name="partitionKey">Name of the partition which can not be null.</param>
		/// <param name="startRowKey">Inclusive start row key. If <c>null</c>, no start range
		/// constraint is enforced.</param>
		/// <param name="endRowKey">Exclusive end row key. If <c>null</c>, no ending range
		/// constraint is enforced.</param>
		/// <remarks>
		/// <para>The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request.
        /// </para>
        /// <para>
        /// The enumeration is ordered by row key.
        /// </para>
        /// <para>
        /// If the partition key does not exists the collection is empty.
        /// </para>
        /// </remarks>
        ///<exception cref="InvalidOperationException"> thrown if the table does not exist.</exception>
		IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey);

		/// <summary>Iterates through all entities specified by their row keys.</summary>
		/// <param name="tableName">The name of the table. This table should exists otherwise the method will fail.</param>
		/// <param name="partitionKey">Partition key (can not be null).</param>
		/// <param name="rowKeys">lazy enumeration of non null string representing rowKeys.</param>
        /// <remarks><para>The enumeration is typically expected to be lazy, iterating through
        /// all the entities with paged request.</para>
        /// <para>If the partition key does not exists the collection is empty.</para>
        /// </remarks>
        ///<exception cref="InvalidOperationException"> thrown if the table does not exist.</exception>
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
		/// <warning>Idempotence is not enforced.</warning>
		/// </remarks>
        ///<exception cref="InvalidOperationException"> thrown if the table does not exist
        /// or an already existing entity has been encountered.</exception>
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
		/// <para>Idempotence of the method is required.</para>
		/// </remarks>
        /// <exception cref="InvalidOperationException"> thrown if the table does not exist
        /// or an non-existing entity has been encountered.</exception>
		void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities);

		// HACK: no 'upsert' (update or insert) available at the time
		// http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/4b902237-7cfb-4d48-941b-4802864fc274

		/// <summary>Deletes all specified entities.</summary>
		/// <param name="partitionKeys">The partition key (assumed to be non null).</param>
		/// <param name="rowKeys">Lazy enumeration of non null string representing the row keys.</param>
		/// <remarks>
		/// <para>
        /// The implementation is expected to lazily iterate through all row keys
        /// and send batch deletion request to the underlying storage.</para>
        /// <para>Idempotence of the method is required.</para>
		/// </remarks>
        ///<exception cref="InvalidOperationException"> thrown if the table does not exist </exception>
		void Delete<T>(string tableName, string partitionKeys, IEnumerable<string> rowKeys);
	}
}
