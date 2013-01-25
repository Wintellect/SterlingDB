using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    ///     Default in-memory driver
    /// </summary>
    public class MemoryDriver : BaseDriver 
    {
        public MemoryDriver()
        {            
        }

        public MemoryDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log) : base(databaseName, serializer, log)
        {
        }

        /// <summary>
        ///     Keys
        /// </summary>
        private readonly Dictionary<Type, object> _keyCache = new Dictionary<Type, object>();

        /// <summary>
        ///     Indexes
        /// </summary>
        private readonly Dictionary<Type, Dictionary<string, object>> _indexCache = new Dictionary<Type, Dictionary<string, object>>();

        /// <summary>
        ///     Objects
        /// </summary>
        private readonly Dictionary<Tuple<string,int>, byte[]> _objectCache = new Dictionary<Tuple<string,int>, byte[]>();

        /// <summary>
        ///     Serialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="keyMap">Key map</param>
        public override void SerializeKeys(Type type, Type keyType, IDictionary keyMap)
        {
            lock (((ICollection)_keyCache).SyncRoot)
            {
                _keyCache[type] = keyMap;
            }
        }

        /// <summary>
        ///     Deserialize keys without generics
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="template">The template</param>
        /// <returns>The keys without the template</returns>
        public override IDictionary DeserializeKeys(Type type, Type keyType, IDictionary template)
        {
            lock (((ICollection)_keyCache).SyncRoot)
            {
                return _keyCache.ContainsKey(type) ? _keyCache[type] as IDictionary : template;
            }
        }

        /// <summary>
        ///     Serialize a single index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>
        public override void SerializeIndex<TKey, TIndex>(Type type, string indexName, Dictionary<TKey, TIndex> indexMap)
        {
            lock(((ICollection)_indexCache).SyncRoot)
            {
                if (!_indexCache.ContainsKey(type))
                {
                    _indexCache.Add(type, new Dictionary<string, object>());
                }

                var indexCache = _indexCache[type];
                indexCache[indexName] = indexMap; 
            }
        }

        /// <summary>
        ///     Serialize a double index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex1">The type of the first index</typeparam>
        /// <typeparam name="TIndex2">The type of the second index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>        
        public override void SerializeIndex<TKey, TIndex1, TIndex2>(Type type, string indexName, Dictionary<TKey, Tuple<TIndex1, TIndex2>> indexMap)
        {
            lock (((ICollection)_indexCache).SyncRoot)
            {
                if (!_indexCache.ContainsKey(type))
                {
                    _indexCache.Add(type, new Dictionary<string, object>());
                }

                var indexCache = _indexCache[type];
                indexCache[indexName] = indexMap;
            }
        }

        /// <summary>
        ///     Deserialize a single index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>
        public override Dictionary<TKey, TIndex> DeserializeIndex<TKey, TIndex>(Type type, string indexName)
        {
            lock (((ICollection)_indexCache).SyncRoot)
            {
                if (!_indexCache.ContainsKey(type))
                    return null;

                var indexCache = _indexCache[type];

                if (!indexCache.ContainsKey(indexName))
                    return null;

                return indexCache[indexName] as Dictionary<TKey, TIndex>;
            }
        }

        /// <summary>
        ///     Deserialize a double index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex1">The type of the first index</typeparam>
        /// <typeparam name="TIndex2">The type of the second index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>        
        public override Dictionary<TKey, Tuple<TIndex1, TIndex2>> DeserializeIndex<TKey, TIndex1, TIndex2>(Type type, string indexName)
        {
            lock (((ICollection)_indexCache).SyncRoot)
            {
                if (!_indexCache.ContainsKey(type))
                    return null;

                var indexCache = _indexCache[type];

                if (!indexCache.ContainsKey(indexName))
                    return null;

                return indexCache[indexName] as Dictionary<TKey, Tuple<TIndex1, TIndex2>>;
            }
        }

        /// <summary>
        ///     Publish the list of tables
        /// </summary>
        /// <param name="tables">The list of tables</param>
        public override void PublishTables(Dictionary<Type, ITableDefinition> tables)
        {
            return;
        }

        /// <summary>
        ///     Serialize the type master
        /// </summary>
        public override void SerializeTypes()
        {
            return;
        }

        /// <summary>
        ///     Save operation
        /// </summary>
        /// <param name="type">Type of the parent</param>
        /// <param name="keyIndex">Index for the key</param>
        /// <param name="bytes">The byte stream</param>
        public override void Save(Type type, int keyIndex, byte[] bytes)
        {
            var key = Tuple.Create(type.FullName, keyIndex);
            lock(((ICollection)_objectCache).SyncRoot)
            {
                _objectCache[key] = bytes;
            }
        }

        /// <summary>
        ///     Load from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        /// <returns>The byte stream</returns>
        public override BinaryReader Load(Type type, int keyIndex)
        {
            var key = Tuple.Create(type.FullName, keyIndex);
            byte[] bytes;
            lock(((ICollection)_objectCache).SyncRoot)
            {
                bytes = _objectCache[key];
            }

            var memStream = new MemoryStream(bytes);
            return new BinaryReader(memStream);
        }

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        public override void Delete(Type type, int keyIndex)
        {
            var key = Tuple.Create(type.FullName, keyIndex);
            lock(((ICollection)_objectCache).SyncRoot)
            {
                if (_objectCache.ContainsKey(key))
                {
                    _objectCache.Remove(key);
                }
            }
        }

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        public override void Truncate(Type type)
        {
            var typeString = type.FullName;
            lock (((ICollection)_objectCache).SyncRoot)
            {
                var keys = from key in _objectCache.Keys where key.Item1.Equals(typeString) select key;
                foreach(var key in keys.ToList())
                {
                    _objectCache.Remove(key);
                }
                
                lock (((ICollection)_indexCache).SyncRoot)
                {
                    var indexes = from index in _indexCache.Keys where _indexCache.ContainsKey(type) select index;
                    foreach(var index in indexes.ToList())
                    {
                        _indexCache.Remove(index);
                    }
                }

                lock (((ICollection)_keyCache).SyncRoot)
                {
                    if (_keyCache.ContainsKey(type))
                    {
                        _keyCache.Remove(type);
                    }
                }
            }                      
        }

        /// <summary>
        ///     Purge the database
        /// </summary>
        public override void Purge()
        {
            var types = from key in _keyCache.Keys select key;
            foreach(var type in types.ToList())
            {
                Truncate(type);
            }
        }
    }
}