#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion


using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Lokad.Cloud.Azure;

namespace Lokad.Cloud.Mock
{

    //[patra]: Under construction. http://code.google.com/p/lokad-cloud/issues/detail?id=98
    /// <summary>Mock in-memory TableStorage Provider.</summary>
    /// <remarks>
    /// All the methods of <see cref="MemoryTableStorageProvider"/> are thread-safe.
    /// </remarks>
    public class MemoryTableStorageProvider : ITableStorageProvider
    {

        Dictionary<string, Dictionary<string, List<FatEntity>>> _tableStorage;
        IBinaryFormatter _formatter;

        public MemoryTableStorageProvider()
        {
            _tableStorage = new Dictionary<string, Dictionary<string, List<FatEntity>>>();
        }
        
        
        public bool CreateTable(string tableName)
        {
            if(_tableStorage.ContainsKey(tableName))
            {
                return false;
            }
            _tableStorage.Add(tableName, new Dictionary<string, List<FatEntity>>());
            return true;
        }

        public bool DeleteTable(string tableName)
        {
            if (_tableStorage.ContainsKey(tableName))
            {
                _tableStorage.Remove(tableName);
                return true;
            }
            return false;
        }

        public IEnumerable<string> GetTables()
        {
            return _tableStorage.Keys;
        }

        public IEnumerable<CloudEntity<T>> Get<T>(string tableName)
        {
            var partitionKeys = _tableStorage[tableName].Keys;

            return partitionKeys.SelectMany(
                pkey => _tableStorage[tableName][pkey].Select(ent => FatEntity.Convert<T>(ent, _formatter)));
            
        }

        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey)
        {
            return _tableStorage[tableName][partitionKey].Select(ent => FatEntity.Convert<T>(ent, _formatter));
        }

        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, string startRowKey, string endRowKey)
        {
            return
                _tableStorage[tableName][partitionKey]
                .Where(ent => (string.Compare(startRowKey,ent.RowKey) <=0 && string.Compare(ent.RowKey, endRowKey) <=0))
                .Select(ent => FatEntity.Convert<T>(ent, _formatter));
        }

        public IEnumerable<CloudEntity<T>> Get<T>(string tableName, string partitionKey, IEnumerable<string> rowKeys)
        {
            return
                _tableStorage[tableName][partitionKey]
                .Where(ent => rowKeys.Contains(ent.RowKey))
                .Select(ent => FatEntity.Convert<T>(ent, _formatter));
        }

        public void Insert<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            throw new NotImplementedException();
        }

        public void Update<T>(string tableName, IEnumerable<CloudEntity<T>> entities)
        {
            throw new NotImplementedException();
        }

        public void Delete<T>(string tableName, string partitionKeys, IEnumerable<string> rowKeys)
        {
            throw new NotImplementedException();
        }
    }
}
