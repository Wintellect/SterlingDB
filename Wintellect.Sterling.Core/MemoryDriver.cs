using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public override Task SerializeKeysAsync(Type type, Type keyType, IDictionary keyMap)
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) _keyCache ).SyncRoot )
                    {
                        _keyCache[ type ] = keyMap;
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Deserialize keys without generics
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="template">The template</param>
        /// <returns>The keys without the template</returns>
        public override Task<IDictionary> DeserializeKeysAsync(Type type, Type keyType, IDictionary template)
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) _keyCache ).SyncRoot )
                    {
                        return _keyCache.ContainsKey( type ) ? _keyCache[ type ] as IDictionary : template;
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Serialize a single index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>
        public override Task SerializeIndexAsync<TKey, TIndex>(Type type, string indexName, Dictionary<TKey, TIndex> indexMap)
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) _indexCache ).SyncRoot )
                    {
                        if ( !_indexCache.ContainsKey( type ) )
                        {
                            _indexCache.Add( type, new Dictionary<string, object>() );
                        }

                        var indexCache = _indexCache[ type ];
                        indexCache[ indexName ] = indexMap;
                    }
                }, TaskCreationOptions.AttachedToParent );
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
        public override Task SerializeIndexAsync<TKey, TIndex1, TIndex2>(Type type, string indexName, Dictionary<TKey, Tuple<TIndex1, TIndex2>> indexMap)
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) _indexCache ).SyncRoot )
                    {
                        if ( !_indexCache.ContainsKey( type ) )
                        {
                            _indexCache.Add( type, new Dictionary<string, object>() );
                        }

                        var indexCache = _indexCache[ type ];
                        indexCache[ indexName ] = indexMap;
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Deserialize a single index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>
        public override Task<Dictionary<TKey, TIndex>> DeserializeIndexAsync<TKey, TIndex>(Type type, string indexName)
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) _indexCache ).SyncRoot )
                    {
                        if ( !_indexCache.ContainsKey( type ) )
                            return null;

                        var indexCache = _indexCache[ type ];

                        if ( !indexCache.ContainsKey( indexName ) )
                            return null;

                        return indexCache[ indexName ] as Dictionary<TKey, TIndex>;
                    }
                }, TaskCreationOptions.AttachedToParent );
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
        public override Task<Dictionary<TKey, Tuple<TIndex1, TIndex2>>> DeserializeIndexAsync<TKey, TIndex1, TIndex2>(Type type, string indexName)
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) _indexCache ).SyncRoot )
                    {
                        if ( !_indexCache.ContainsKey( type ) )
                            return null;

                        var indexCache = _indexCache[ type ];

                        if ( !indexCache.ContainsKey( indexName ) )
                            return null;

                        return indexCache[ indexName ] as Dictionary<TKey, Tuple<TIndex1, TIndex2>>;
                    }
                }, TaskCreationOptions.AttachedToParent );
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
        public override Task SerializeTypesAsync()
        {
            return Task.Factory.StartNew( () => { }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Save operation
        /// </summary>
        /// <param name="type">Type of the parent</param>
        /// <param name="keyIndex">Index for the key</param>
        /// <param name="bytes">The byte stream</param>
        public override Task SaveAsync(Type type, int keyIndex, byte[] bytes)
        {
            return Task.Factory.StartNew( () =>
                {
                    var key = Tuple.Create( type.FullName, keyIndex );
                    lock ( ( (ICollection) _objectCache ).SyncRoot )
                    {
                        _objectCache[ key ] = bytes;
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Load from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        /// <returns>The byte stream</returns>
        public override Task<BinaryReader> LoadAsync(Type type, int keyIndex)
        {
            return Task.Factory.StartNew( () =>
                {
                    var key = Tuple.Create( type.FullName, keyIndex );
                    byte[] bytes;
                    lock ( ( (ICollection) _objectCache ).SyncRoot )
                    {
                        bytes = _objectCache[ key ];
                    }

                    var memStream = new MemoryStream( bytes );
                    return new BinaryReader( memStream );
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        public override Task DeleteAsync(Type type, int keyIndex)
        {
            return Task.Factory.StartNew( () =>
                {
                    var key = Tuple.Create( type.FullName, keyIndex );
                    lock ( ( (ICollection) _objectCache ).SyncRoot )
                    {
                        if ( _objectCache.ContainsKey( key ) )
                        {
                            _objectCache.Remove( key );
                        }
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        public override Task TruncateAsync(Type type)
        {
            return Task.Factory.StartNew( () =>
                {
                    var typeString = type.FullName;
                    lock ( ( (ICollection) _objectCache ).SyncRoot )
                    {
                        var keys = from key in _objectCache.Keys where key.Item1.Equals( typeString ) select key;
                        foreach ( var key in keys.ToList() )
                        {
                            _objectCache.Remove( key );
                        }

                        lock ( ( (ICollection) _indexCache ).SyncRoot )
                        {
                            var indexes = from index in _indexCache.Keys where _indexCache.ContainsKey( type ) select index;
                            foreach ( var index in indexes.ToList() )
                            {
                                _indexCache.Remove( index );
                            }
                        }

                        lock ( ( (ICollection) _keyCache ).SyncRoot )
                        {
                            if ( _keyCache.ContainsKey( type ) )
                            {
                                _keyCache.Remove( type );
                            }
                        }
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Purge the database
        /// </summary>
        public override Task PurgeAsync()
        {
            return Task.Factory.StartNew( () =>
                {
                    var types = from key in _keyCache.Keys select key;
                    
                    foreach ( var type in types.ToList() )
                    {
                        TruncateAsync( type );
                    }
                }, TaskCreationOptions.AttachedToParent );
        }
    }
}