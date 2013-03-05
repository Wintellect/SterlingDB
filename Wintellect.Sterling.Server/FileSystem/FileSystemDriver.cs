using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Server.FileSystem
{
    /// <summary>
    ///     Default driver for isolated storage
    /// </summary>
    public class FileSystemDriver : BaseDriver
    {
        private const string BASE = "Databases/";
        private readonly List<Type> _tables = new List<Type>();
        private bool _dirtyType;
        
        public FileSystemDriver() : this(BASE)
        {            
        }

        public FileSystemDriver(string basePath) 
        {
            Initialize(basePath);
        }

        public FileSystemDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log) : this(databaseName, serializer, log, BASE)
        {
        }
        
        public FileSystemDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log, string basePath)
            : base(databaseName, serializer, log)
        {
            Initialize(basePath);
        }

        private FileSystemHelper _fileHelper;
        private string _basePath;
        private readonly PathProvider _pathProvider = new PathProvider();

        public void Initialize(string basePath)
        {
            _fileHelper = new FileSystemHelper();
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
            _fileHelper.EnsureDirectory( _pathProvider.GetTablePath( _basePath, DatabaseName, type, this ) );

            var pathLock = PathLock.GetLock( type.FullName );

            using ( await pathLock.LockAsync() )
            {
                var keyPath = _pathProvider.GetKeysPath( _basePath, DatabaseName, type, this );

                using ( var keyFile = _fileHelper.GetWriter( keyPath ) )
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
            var keyPath = _pathProvider.GetKeysPath( _basePath, DatabaseName, type, this );

            if ( _fileHelper.FileExists( keyPath ) )
            {
                var pathLock = PathLock.GetLock( type.FullName );

                using ( await pathLock.LockAsync() )
                {
                    using ( var keyFile = _fileHelper.GetReader( keyPath ) )
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
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );

            var pathLock = PathLock.GetLock( type.FullName );
            
            using( await pathLock.LockAsync() )
            {
                using ( var indexFile = _fileHelper.GetWriter( indexPath ) )
                {
                    indexFile.Write( indexMap.Count );

                    foreach ( var index in indexMap )
                    {
                        DatabaseSerializer.Serialize( index.Key, indexFile );
                        DatabaseSerializer.Serialize( index.Value, indexFile );
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
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );

            var pathLock = PathLock.GetLock( type.FullName );
            
            using( pathLock.LockAsync() )
            {
                using ( var indexFile = _fileHelper.GetWriter( indexPath ) )
                {
                    indexFile.Write( indexMap.Count );

                    foreach ( var index in indexMap )
                    {
                        DatabaseSerializer.Serialize( index.Key, indexFile );
                        DatabaseSerializer.Serialize( index.Value.Item1, indexFile );
                        DatabaseSerializer.Serialize( index.Value.Item2, indexFile );
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
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );

            var dictionary = new Dictionary<TKey, TIndex>();
            
            if ( _fileHelper.FileExists( indexPath ) )
            {
                var pathLock = PathLock.GetLock( type.FullName );
            
                using( await pathLock.LockAsync() )
                {
                    using ( var indexFile = _fileHelper.GetReader( indexPath ) )
                    {
                        var count = indexFile.ReadInt32();

                        for ( var x = 0; x < count; x++ )
                        {
                            dictionary.Add( (TKey) DatabaseSerializer.Deserialize( typeof( TKey ), indexFile ),
                                           (TIndex) DatabaseSerializer.Deserialize( typeof( TIndex ), indexFile ) );
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
            var indexPath = _pathProvider.GetIndexPath( _basePath, DatabaseName, type, this, indexName );

            var dictionary = new Dictionary<TKey, Tuple<TIndex1, TIndex2>>();
            
            if ( _fileHelper.FileExists( indexPath ) )
            {
                var pathLock = PathLock.GetLock( type.FullName );
            
                using( await pathLock.LockAsync() )
                {
                    using ( var indexFile = _fileHelper.GetReader( indexPath ) )
                    {
                        var count = indexFile.ReadInt32();

                        for ( var x = 0; x < count; x++ )
                        {
                            dictionary.Add( (TKey) DatabaseSerializer.Deserialize( typeof( TKey ), indexFile ),
                                Tuple.Create(
                                (TIndex1) DatabaseSerializer.Deserialize( typeof( TIndex1 ), indexFile ),
                                (TIndex2) DatabaseSerializer.Deserialize( typeof( TIndex2 ), indexFile ) ) );
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
        public override async void PublishTables(Dictionary<Type, ITableDefinition> tables, Func<string, Type> resolveType )
        {
            _fileHelper.EnsureDirectory(_pathProvider.GetDatabasePath(_basePath, DatabaseName, this));

            var typePath = _pathProvider.GetTypesPath(_basePath, DatabaseName, this);

            if (!_fileHelper.FileExists(typePath)) return;

            using (var typeFile = _fileHelper.GetReader(typePath))
            {
                var count = typeFile.ReadInt32();

                for (var x = 0; x < count; x++)
                {
                    var fullTypeName = typeFile.ReadString();

                    var tableType = resolveType(fullTypeName);
                    
                    if (tableType == null)
                    {
                        throw new SterlingTableNotFoundException(fullTypeName, DatabaseName);
                    }

                    await GetTypeIndexAsync( tableType.AssemblyQualifiedName );
                }
            }

            var pathLock = PathLock.GetLock(DatabaseName);
            
            using ( await pathLock.LockAsync() )
            {
                foreach (var type in tables.Keys)
                {
                    _tables.Add(type);
                    _fileHelper.EnsureDirectory(_pathProvider.GetTablePath(_basePath, DatabaseName, type, this));
                }
            }
        }

        /// <summary>
        ///     Serialize the type master
        /// </summary>
        public override async Task SerializeTypesAsync()
        {
            var pathLock = PathLock.GetLock( TypeIndex.GetType().FullName );

            using( await pathLock.LockAsync() )
            {
                var typePath = _pathProvider.GetTypesPath( _basePath, DatabaseName, this );

                using ( var typeFile = _fileHelper.GetWriter( typePath ) )
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
            var pathLock = PathLock.GetLock( TypeIndex.GetType().FullName );

            using ( await pathLock.LockAsync() )
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
            var instanceFolder = _pathProvider.GetInstanceFolder( _basePath, DatabaseName, type, this, keyIndex );

            _fileHelper.EnsureDirectory( instanceFolder );
            
            var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseName, type, this, keyIndex );

            var pathLock = PathLock.GetLock( instancePath );

            using( await pathLock.LockAsync() )
            using ( var instanceFile = _fileHelper.GetWriter( instancePath ) )
            {
                instanceFile.Write( bytes );
                instanceFile.Flush();
                instanceFile.Close();
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
            var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseName, type, this, keyIndex );

            var pathLock = PathLock.GetLock( instancePath );

            using( await pathLock.LockAsync() )
            {
                return _fileHelper.FileExists( instancePath )
                           ? _fileHelper.GetReader( instancePath )
                           : new BinaryReader( new MemoryStream() );
            }
        }

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        public override async Task DeleteAsync(Type type, int keyIndex)
        {
            var instancePath = _pathProvider.GetInstancePath( _basePath, DatabaseName, type, this, keyIndex );

            var pathLock = PathLock.GetLock( instancePath );

            using( await pathLock.LockAsync() )
            {
                if ( _fileHelper.FileExists( instancePath ) )
                {
                    _fileHelper.Delete( instancePath );
                }
            }
        }

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        public override async Task TruncateAsync(Type type)
        {
            var folderPath = _pathProvider.GetTablePath( _basePath, DatabaseName, type, this );

            var pathLock = PathLock.GetLock( type.FullName );

            using ( await pathLock.LockAsync() )
            {
                _fileHelper.Purge( folderPath );
            }
        }

        /// <summary>
        ///     Purge the database
        /// </summary>
        public override async Task PurgeAsync()
        {
            var pathLock = PathLock.GetLock( DatabaseName );

            using ( await pathLock.LockAsync() )
            {
                _fileHelper.Purge( _pathProvider.GetDatabasePath( _basePath, DatabaseName, this ) );
            }
        }        
    }
}