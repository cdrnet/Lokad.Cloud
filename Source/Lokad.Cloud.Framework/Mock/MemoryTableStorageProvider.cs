#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Azure;

namespace Lokad.Cloud.Mock
{
    /// <summary>Mock in-memory TableStorage Provider.</summary>
    /// <remarks>
    /// All the methods of <see cref="MemoryTableStorageProvider"/> are thread-safe.
    /// </remarks>
    public class MemoryTableStorageProvider : ITableStorageProvider
    {
        //In memory table storage : triple nested dictionaries.
        //First level : access indexed tables.
        //Second level : access indexed partitions. (partitionKey)
        //Third level : access indexed entities. (rowKey)
        readonly Dictionary<string, Dictionary<string, Dictionary<string,FatEntity>>> _tableStorage;

        //A formatter is requiered to handle FatEntities.
    	readonly IBinaryFormatter _formatter;

        /// <summary>naive global lock to make methods thread-safe.</summary>
        readonly object _syncRoot;

        /// <summary>
        /// Constructor for <see cref="MemoryTableStorageProvider"/>.
        /// </summary>
        public MemoryTableStorageProvider()
        {
            _tableStorage = new Dictionary<string, Dictionary<string, Dictionary<string, FatEntity>>>();
            _syncRoot = new object();
            _formatter = new CloudFormatter();
        }

        /// <see cref="ITableStorageProvider.CreateTable"/>
        public bool CreateTable(string tableName)
        {
            lock (_syncRoot)
            {
                if (_tableStorage.ContainsKey(tableName))
                {
                    //If the table already exists: return false.
                    return false;
                }
                //create table return true.
                _tableStorage.Add(tableName, new Dictionary<string, Dictionary<string, FatEntity>>());
                return true;
            }
        }

        /// <see cref="ITableStorageProvider.DeleteTable"/>
        public bool DeleteTable(string tableName)
        {
            lock (_syncRoot)
            {
                if (_tableStorage.ContainsKey(tableName))
                {
                    //If the table exists remove it.
                    _tableStorage.Remove(tableName);
                    return true;
                }
                //Can not remove an unexisting table.
                return false;
            }
        }

        /// <see cref="ITableStorageProvider.GetTables"/>
        public IEnumerable<string> GetTables()
        {
            lock (_syncRoot)
            { 
                return _tableStorage.Keys;
            }  
        }

        /// <see cref="ITableStorageProvider.Get{T}(string)"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName)
        {
            lock (_syncRoot)
            {
                if(!_tableStorage.ContainsKey(tableName))
                {
                    return new List<CloudEntity<T>>();
                }

                return _tableStorage[tableName].Values
                       .SelectMany(dict => dict.Values.Select(ent => FatEntity.Convert<T>(ent, _formatter)));
              
            }

        }

        /// <see cref="ITableStorageProvider.Get{T}(string,string)"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey)
        {
            lock (_syncRoot)
            {   
                //If tableName or partitionKey does not exist the method is supposed to return an empty collection.
                if(_tableStorage.ContainsKey(tableName) && _tableStorage[tableName].ContainsKey(partitionKey))
                {
                    return _tableStorage[tableName][partitionKey].Values
                            .Select(ent => FatEntity.Convert<T>(ent, _formatter));
                }
                return new List<CloudEntity<T>>();
            }
        }

        /// <see cref="ITableStorageProvider.Get{T}(string,string,string,string)"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey)
        {
            lock (_syncRoot)
            {
                //If tableName or partitionKey does not exist the method is supposed to return an empty collection.
                if (_tableStorage.ContainsKey(tableName) && _tableStorage[tableName].ContainsKey(partitionKey))
                {
                    return _tableStorage[tableName][partitionKey].Where(
                        pair => 
                            (string.Compare(startRowKey, pair.Key) <= 0 )
                            && (string.IsNullOrEmpty(endRowKey) ? true : string.Compare(pair.Key, endRowKey) < 0))
                        .OrderBy(pair => pair.Key)
                        .Select(pair => FatEntity.Convert<T>(pair.Value, _formatter));
                }
                return new List<CloudEntity<T>>();
            }
        }

        /// <see cref="ITableStorageProvider.Get{T}(string,string,System.Collections.Generic.IEnumerable{string})"/>
        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
        {
            lock (_syncRoot)
            {
                //If tableName or partitionKey does not exist the method is supposed to return an empty collection.
                if (_tableStorage.ContainsKey(tableName) && _tableStorage[tableName].ContainsKey(partitionKey))
                {
                    //Retrieves the partition.
                    var partition = _tableStorage[tableName][partitionKey];
                    var entities = new List<CloudEntity<T>>();
                    //If the rowKeys exists then return the associated entity.
                    foreach (var rowKey in rowKeys)
                    {
                        FatEntity fatEntity;
                        if(partition.TryGetValue(rowKey, out fatEntity))
                        {
                           entities.Add(FatEntity.Convert<T>(fatEntity, _formatter));
                        }
                    }
                    return entities;
                }
                return new List<CloudEntity<T>>();
            }

        }

        /// <see cref="ITableStorageProvider.Insert{T}"/>
        public void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            lock (_syncRoot)
            {
                foreach (var entity in entities)
                {
                    //If table does not exist then we have to create it.
                    if(!_tableStorage.ContainsKey(tableName))
                    {
                        _tableStorage.Add(tableName, new Dictionary<string, Dictionary<string, FatEntity>>());
                    }
                    
                    //If the partitionKey does not exist then we have to create it.
                    if (!_tableStorage[tableName].ContainsKey(entity.PartitionKey))
                    {
                        _tableStorage[tableName].Add(entity.PartitionKey, new Dictionary<string, FatEntity>());
                        _tableStorage[tableName][entity.PartitionKey].Add(entity.RowRey,
                            FatEntity.Convert(entity, _formatter));
                    }
                    else
                    {
                        if(!_tableStorage[tableName][entity.PartitionKey].ContainsKey(entity.RowRey))
                        {
                            _tableStorage[tableName][entity.PartitionKey].Add(entity.RowRey,
                           FatEntity.Convert(entity, _formatter));
                        }
                        // In this case both partitionKey and rowKey exist then the method is supposed to fail.
                        else
                        {
                            throw new MockTableStorageException("Insert is impossible : already existing entities.");
                        }
                    }
                }
            }
        }

        /// <see cref="ITableStorageProvider.Update{T}"/>
        public void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            lock (_syncRoot)
            {
                foreach (var entity in entities)
                {
                    //The method fails at the first non existing entity.
                    if( _tableStorage[tableName].ContainsKey(entity.PartitionKey)
                        && _tableStorage[tableName][entity.PartitionKey].ContainsKey(entity.RowRey))
                        
                        _tableStorage[tableName][entity.PartitionKey][entity.RowRey] = FatEntity.Convert(entity,
                            _formatter);
                    else
                    {
                         throw new MockTableStorageException("Update is impossible : non existing entities.");
                    }
                }
            }
        }
		/// <see cref="ITableStorageProvider.Update{T}"/>
        public void Upsert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			lock (_syncRoot)
			{
				// deleting all existing entities
				entities.GroupBy(e => e.PartitionKey)
					.ForEach(g => Delete<T>(tableName, g.Key, g.Select(e => e.RowRey)));
				
				// inserting all entities
				Insert(tableName, entities);
			}
		}

        /// <see cref="ITableStorageProvider.Delete{T}"/>
        public void Delete<T>(string tableName, string partitionKeys, IEnumerable<string> rowKeys)
        {
            lock (_syncRoot)
            {
                //Iteration on rowKey is necessary only if partitionKey exist.
                if (_tableStorage.ContainsKey(tableName) && _tableStorage[tableName].ContainsKey(partitionKeys))
                {
                    foreach (var key in rowKeys)
                    {
                        if (_tableStorage[tableName][partitionKeys].ContainsKey(key))
                        {
                            _tableStorage[tableName][partitionKeys].Remove(key);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exception raised my MemoryTableStorageProvider.
        /// </summary>
        class MockTableStorageException : InvalidOperationException
        {
            public MockTableStorageException(string message) : base(message){}
        }
    }
}
