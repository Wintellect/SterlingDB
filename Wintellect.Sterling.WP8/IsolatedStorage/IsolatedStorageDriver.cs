
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.WP8.IsolatedStorage
{
    /// <summary>
    ///     Default driver for isolated storage
    /// </summary>
    public class IsolatedStorageDriver : BaseDriver
    {
        private const string BASE = "Sterling/";
        private readonly List<Type> _tables = new List<Type>();
        private bool _dirtyType;
        private readonly AsyncLock _lock = new AsyncLock();
        
        public IsolatedStorageDriver() : this(BASE, false)
        {            
        }

        public IsolatedStorageDriver(string basePath) : this(basePath, false)
        {            
        }

        public IsolatedStorageDriver(string basePath, bool siteWide)
        {       
            Initialize(basePath, siteWide);
        }

        private IsoStorageHelper _iso;
        private string _basePath;
        private readonly PathProvider _pathProvider = new PathProvider();

        public void Initialize(string basePath, bool siteWide)
        {
            _iso = new IsoStorageHelper(siteWide);
            _basePath = basePath.EndsWith( "/" ) ? basePath : basePath + "/";
        }

        /// <summary>
        ///     Serialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="keyMap">Key map</param>
        public override async Task SerializeKeysAsync(Type type, Type keyType, IDictionary keyMap)
        {
            _iso.EnsureDirectory( _pathProvider.GetTablePath( _basePath, DatabaseInstanceName, type, this ) );

            using ( await _lock.LockAsync() )
            {
                var keyPath = _pathProvider.GetKeysPath( _basePath, DatabaseInstanceName, type, this );

                using ( var keyFile = _iso.GetWriter( keyPath ) )
                {
                    keyFile.Write( keyMap.Count );
                
                    foreach ( var key in keyMap.Keys )
                    {
                        DatabaseSerializer.Serialize( key, keyFile );
                        keyFile.Write( (int) keyMap[ key ] );
                    }
                }
            }

            await SerializeTypesAsync();
        }

        /// <summary>
        ///     Deserialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="dictionary">Empty dictionary</param>
        /// <returns>The key list</returns>
        public override async Task<IDictionary> DeserializeKeysAsync(Type type, Type keyType, IDictionary dictionary)
        {
            var keyPath = _pathProvider.GetKeysPath( _basePath, DatabaseInstanceName, type, this );

            if ( _iso.FileExists( keyPath ) )
            {
                using ( await _lock.LockAsync() )
                {
                    using ( var keyFile = _iso.GetReader( keyPath ) )
                    {
                        var count = keyFile.ReadInt32();

                        for ( var x = 0; x < count; x++ )
                        {
                            dictionary.Add( DatabaseSerializer.Deserialize( keyType, keyFile ), keyFile.ReadInt32() );
                        }
                    }
                }
            }
            return dictionary;
        }

        /// <summary>
        ///     Serialize a single index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>
        public override async Task SerializeIndexAsync<TKey, TIndex>(Type type, string indexName, Dictionary<TKey, TIndex> indexMap)
        {
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseInstanceName, type, this, indexName );

            using ( await _lock.LockAsync() )
            {
                using ( var indexFile = _iso.GetWriter( indexPath ) )
                {
                    indexFile.Write( indexMap.Count );

                    foreach ( var index in indexMap )
                    {
                        DatabaseSerializer.Serialize( index.Value, indexFile );
                        DatabaseSerializer.Serialize( index.Key, indexFile );
                    }
                }
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
        public override async Task SerializeIndexAsync<TKey, TIndex1, TIndex2>(Type type, string indexName, Dictionary<TKey, Tuple<TIndex1, TIndex2>> indexMap)
        {
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseInstanceName, type, this, indexName );

            using ( await _lock.LockAsync() )
            {
                using ( var indexFile = _iso.GetWriter( indexPath ) )
                {
                    indexFile.Write( indexMap.Count );

                    foreach ( var index in indexMap )
                    {
                        DatabaseSerializer.Serialize( index.Value.Item1, indexFile );
                        DatabaseSerializer.Serialize( index.Value.Item2, indexFile );
                        DatabaseSerializer.Serialize( index.Key, indexFile );
                    }
                }
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
        public override async Task<Dictionary<TKey, TIndex>> DeserializeIndexAsync<TKey, TIndex>(Type type, string indexName)
        {
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseInstanceName, type, this, indexName );

            var dictionary = new Dictionary<TKey, TIndex>();
            
            if ( _iso.FileExists( indexPath ) )
            {
                using ( await _lock.LockAsync() )
                {
                    using ( var indexFile = _iso.GetReader( indexPath ) )
                    {
                        var count = indexFile.ReadInt32();

                        for ( var x = 0; x < count; x++ )
                        {
                            var index = (TIndex) DatabaseSerializer.Deserialize( typeof( TIndex ), indexFile );
                            var key = (TKey) DatabaseSerializer.Deserialize( typeof( TKey ), indexFile );
                        
                            dictionary.Add( key, index );
                        }
                    }
                }
            }
            return dictionary;
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
        public override async Task<Dictionary<TKey, Tuple<TIndex1, TIndex2>>> DeserializeIndexAsync<TKey, TIndex1, TIndex2>(Type type, string indexName)
        {
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseInstanceName, type, this, indexName );

            var dictionary = new Dictionary<TKey, Tuple<TIndex1, TIndex2>>();
            
            if ( _iso.FileExists( indexPath ) )
            {
                using ( await _lock.LockAsync() )
                {
                    using ( var indexFile = _iso.GetReader( indexPath ) )
                    {
                        var count = indexFile.ReadInt32();

                        for ( var x = 0; x < count; x++ )
                        {
                            var index = Tuple.Create(
                                (TIndex1) DatabaseSerializer.Deserialize( typeof( TIndex1 ), indexFile ),
                                (TIndex2) DatabaseSerializer.Deserialize( typeof( TIndex2 ), indexFile ) );
                        
                            var key = (TKey) DatabaseSerializer.Deserialize( typeof( TKey ), indexFile );
                            
                            dictionary.Add( key, index );
                        }
                    }
                }
            }
            return dictionary;
        }

        /// <summary>
        ///     Publish the list of tables
        /// </summary>
        /// <param name="tables">The list of tables</param>
        public override async void PublishTables( Dictionary<Type, ITableDefinition> tables, Func<string, Type> resolveType )
        {
            _iso.EnsureDirectory(_pathProvider.GetDatabasePath(_basePath, DatabaseInstanceName, this));

            var typePath = _pathProvider.GetTypesPath(_basePath, DatabaseInstanceName, this);

            if (!_iso.FileExists(typePath)) return;

            using (var typeFile = _iso.GetReader(typePath))
            {
                var count = typeFile.ReadInt32();

                for (var x = 0; x < count; x++)
                {
                    var fullTypeName = typeFile.ReadString();
                    var tableType = resolveType(fullTypeName);
                    if (tableType == null)
                    {
                        throw new SterlingTableNotFoundException(fullTypeName, DatabaseInstanceName);
                    }

                    await GetTypeIndexAsync(tableType.AssemblyQualifiedName);
                }
            }

            using ( await _lock.LockAsync() )
            {
                foreach (var type in tables.Keys)
                {
                    _tables.Add(type);
                    _iso.EnsureDirectory(_pathProvider.GetTablePath(_basePath, DatabaseInstanceName, type, this));
                }
            }
        }

        /// <summary>
        ///     Serialize the type master
        /// </summary>
        public override async Task SerializeTypesAsync()
        {
            using ( await _lock.LockAsync() )
            {
                var typePath = _pathProvider.GetTypesPath( _basePath, DatabaseInstanceName, this );
            
                using ( var typeFile = _iso.GetWriter( typePath ) )
                {
                    typeFile.Write( TypeIndex.Count );

                    foreach ( var type in TypeIndex )
                    {
                        typeFile.Write( type );
                    }
                }
            }
        }

        /// <summary>
        ///     Get the index for the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public override async Task<int> GetTypeIndexAsync(string type)
        {
            using ( await _lock.LockAsync() )
            {
                if ( !TypeIndex.Contains( type ) )
                {
                    TypeIndex.Add( type );
                    _dirtyType = true;
                }
            }

            return TypeIndex.IndexOf( type );
        }

        /// <summary>
        ///     Get the type at an index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The type</returns>
        public override Task<string> GetTypeAtIndexAsync(int index)
        {
            return Task.FromResult( TypeIndex[ index ] );
        }
        
        /// <summary>
        ///     Save operation
        /// </summary>
        /// <param name="type">Type of the parent</param>
        /// <param name="keyIndex">Index for the key</param>
        /// <param name="bytes">The byte stream</param>
        public override async Task SaveAsync(Type type, int keyIndex, byte[] bytes)
        {
            var instanceFolder = _pathProvider.GetInstanceFolder( _basePath, DatabaseInstanceName, type, this, keyIndex );

            _iso.EnsureDirectory( instanceFolder );
            
            var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseInstanceName, type, this, keyIndex );

            using ( await _lock.LockAsync() )
            using ( var instanceFile = _iso.GetWriter( instancePath ) )
            {
                instanceFile.Write( bytes );
            }

            if ( !_dirtyType ) return;

            _dirtyType = false;

            await SerializeTypesAsync();
        }   
            
        /// <summary>
        ///     Load from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        /// <returns>The byte stream</returns>
        public override async Task<BinaryReader> LoadAsync(Type type, int keyIndex)
        {
            var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseInstanceName, type, this, keyIndex );

            using ( await _lock.LockAsync() )
            {
                return _iso.FileExists( instancePath ) ? _iso.GetReader( instancePath ) : new BinaryReader( new MemoryStream() );
            }
        }

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        public override async Task DeleteAsync(Type type, int keyIndex)
        {
            var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseInstanceName, type, this, keyIndex );

            using ( await _lock.LockAsync() )
            {
                if ( _iso.FileExists( instancePath ) )
                {
                    _iso.Delete( instancePath );
                }
            }
        }

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        public override async Task TruncateAsync(Type type)
        {
            var folderPath = _pathProvider.GetTablePath( _basePath, DatabaseInstanceName, type, this );

            using ( await _lock.LockAsync() )
            {
                _iso.Purge( folderPath );
            }
        }

        /// <summary>
        ///     Purge the database
        /// </summary>
        public override async Task PurgeAsync()
        {
            using ( await _lock.LockAsync() )
            {
                _iso.Purge( _pathProvider.GetDatabasePath( _basePath, DatabaseInstanceName, this ) );
            }
        }        
    }
}