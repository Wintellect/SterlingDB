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
        private readonly Dictionary<string,byte[]> _saveCache = new Dictionary<string, byte[]>();
        
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

        public IsolatedStorageDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log) : this(databaseName, serializer, log, false)
        {
        }

        public IsolatedStorageDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log, bool siteWide) 
            : this(databaseName, serializer, log, siteWide, BASE)
        {           
        }

        public IsolatedStorageDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log, bool siteWide, string basePath)
            : base(databaseName, serializer, log)
        {
            Initialize(basePath, siteWide);
        }

        private IsoStorageHelper _iso;
        private string _basePath;
        private readonly PathProvider _pathProvider = new PathProvider();

        public void Initialize(string basePath, bool siteWide)
        {
            _iso = new IsoStorageHelper(siteWide);            
            _basePath = basePath; 
        }

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
                    _iso.EnsureDirectory( _pathProvider.GetTablePath( _basePath, DatabaseName, type, this ) );
                    var pathLock = PathLock.GetLock( type.FullName );
                    lock ( pathLock )
                    {
                        var keyPath = _pathProvider.GetKeysPath( _basePath, DatabaseName, type, this );
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
                    
                    SerializeTypesAsync();
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Deserialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="dictionary">Empty dictionary</param>
        /// <returns>The key list</returns>
        public override Task<IDictionary> DeserializeKeysAsync(Type type, Type keyType, IDictionary dictionary)
        {
            return Task.Factory.StartNew( () =>
                {
                    var keyPath = _pathProvider.GetKeysPath( _basePath, DatabaseName, type, this );
                    if ( _iso.FileExists( keyPath ) )
                    {
                        var pathLock = PathLock.GetLock( type.FullName );
                        lock ( pathLock )
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
                    var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );
                    var pathLock = PathLock.GetLock( type.FullName );
                    lock ( pathLock )
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
                    var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );
                    var pathLock = PathLock.GetLock( type.FullName );
                    lock ( pathLock )
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
                    var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );
                    var dictionary = new Dictionary<TKey, TIndex>();
                    if ( _iso.FileExists( indexPath ) )
                    {
                        var pathLock = PathLock.GetLock( type.FullName );
                        lock ( pathLock )
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
                    var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );
                    var dictionary = new Dictionary<TKey, Tuple<TIndex1, TIndex2>>();
                    if ( _iso.FileExists( indexPath ) )
                    {
                        var pathLock = PathLock.GetLock( type.FullName );
                        lock ( pathLock )
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
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Publish the list of tables
        /// </summary>
        /// <param name="tables">The list of tables</param>
        public override void PublishTables(Dictionary<Type, ITableDefinition> tables)
        {
            _iso.EnsureDirectory(_pathProvider.GetDatabasePath(_basePath, DatabaseName, this));

            var typePath = _pathProvider.GetTypesPath(_basePath, DatabaseName, this);

            if (!_iso.FileExists(typePath)) return;

            using (var typeFile = _iso.GetReader(typePath))
            {
                var count = typeFile.ReadInt32();
                for (var x = 0; x < count; x++)
                {
                    var fullTypeName = typeFile.ReadString();
                    var tableType = TableTypeResolver.ResolveTableType(fullTypeName);
                    if (tableType == null)
                    {
                        throw new SterlingTableNotFoundException(fullTypeName, DatabaseName);
                    }

                    GetTypeIndexAsync(tableType.AssemblyQualifiedName).Wait();
                }
            }

            var pathLock = PathLock.GetLock(DatabaseName);
            lock (pathLock)
            {
                foreach (var type in tables.Keys)
                {
                    _tables.Add(type);
                    _iso.EnsureDirectory(_pathProvider.GetTablePath(_basePath, DatabaseName, type, this));
                }
            }
        }

        /// <summary>
        ///     Serialize the type master
        /// </summary>
        public override Task SerializeTypesAsync()
        {
            return Task.Factory.StartNew( () =>
                {
                    var pathLock = PathLock.GetLock( TypeIndex.GetType().FullName );
                    lock ( pathLock )
                    {
                        var typePath = _pathProvider.GetTypesPath( _basePath, DatabaseName, this );
                        using ( var typeFile = _iso.GetWriter( typePath ) )
                        {
                            typeFile.Write( TypeIndex.Count );
                            foreach ( var type in TypeIndex )
                            {
                                typeFile.Write( type );
                            }
                        }
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Get the index for the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public override Task<int> GetTypeIndexAsync(string type)
        {
            return Task.Factory.StartNew( () =>
                {
                    var pathLock = PathLock.GetLock( TypeIndex.GetType().FullName );
                    lock ( pathLock )
                    {
                        if ( !TypeIndex.Contains( type ) )
                        {
                            TypeIndex.Add( type );
                            _dirtyType = true;
                        }
                    }
                    return TypeIndex.IndexOf( type );
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Get the type at an index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The type</returns>
        public override Task<string> GetTypeAtIndexAsync(int index)
        {
            return Task.Factory.StartNew( () => TypeIndex[ index ], TaskCreationOptions.AttachedToParent );
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
                    var instanceFolder = _pathProvider.GetInstanceFolder( _basePath, DatabaseName, type, this, keyIndex );
                    _iso.EnsureDirectory( instanceFolder );
                    var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseName, type, this, keyIndex );

                    lock ( ( (ICollection) _saveCache ).SyncRoot )
                    {
                        _saveCache[ instancePath ] = bytes;
                    }

                    // lock on this while saving, but remember that anyone else loading can now grab the
                    // copy 
                    lock ( PathLock.GetLock( instancePath ) )
                    {
                        using (
                            var instanceFile =
                                _iso.GetWriter( instancePath ) )
                        {
                            instanceFile.Write( bytes );
                        }

                        lock ( ( (ICollection) _saveCache ).SyncRoot )
                        {
                            if ( _saveCache.ContainsKey( instancePath ) )
                            {
                                _saveCache.Remove( instancePath );
                            }
                        }
                    }

                    if ( !_dirtyType ) return;

                    _dirtyType = false;

                    SerializeTypesAsync();
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
                    var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseName, type, this, keyIndex );

                    // if a copy is available, return that - it's our best bet
                    lock ( ( (ICollection) _saveCache ).SyncRoot )
                    {
                        if ( _saveCache.ContainsKey( instancePath ) )
                        {
                            return new BinaryReader( new MemoryStream( _saveCache[ instancePath ] ) );
                        }
                    }

                    // otherwise let's wait for it to be released and grab it from disk
                    lock ( PathLock.GetLock( instancePath ) )
                    {
                        return _iso.FileExists( instancePath )
                                   ? _iso.GetReader( instancePath )
                                   : new BinaryReader( new MemoryStream() );
                    }
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
                    var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseName, type, this, keyIndex );
                    lock ( PathLock.GetLock( instancePath ) )
                    {
                        if ( _iso.FileExists( instancePath ) )
                        {
                            _iso.Delete( instancePath );
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
                    var folderPath = _pathProvider.GetTablePath( _basePath, DatabaseName, type, this );
                    lock ( PathLock.GetLock( type.FullName ) )
                    {
                        _iso.Purge( folderPath );
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
                    lock ( PathLock.GetLock( DatabaseName ) )
                    {
                        _iso.Purge( _pathProvider.GetDatabasePath( _basePath, DatabaseName, this ) );
                    }
                }, TaskCreationOptions.AttachedToParent );
        }        
    }
}