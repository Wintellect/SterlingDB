using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            _basePath = basePath; 
        }

        /// <summary>
        ///     Serialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="keyMap">Key map</param>
        public override void SerializeKeys(Type type, Type keyType, IDictionary keyMap)
        {
            _fileHelper.EnsureDirectory(_pathProvider.GetTablePath(_basePath, DatabaseName, type, this));
            var pathLock = PathLock.GetLock(type.FullName);
            lock(pathLock)
            {
                var keyPath = _pathProvider.GetKeysPath(_basePath, DatabaseName, type, this);
                using (var keyFile = _fileHelper.GetWriter(keyPath))
                {
                    keyFile.Write(keyMap.Count);
                    foreach(var key in keyMap.Keys)
                    {
                        DatabaseSerializer.Serialize(key, keyFile);
                        keyFile.Write((int)keyMap[key]);
                    }
                }
            }
            SerializeTypes();
        }

        /// <summary>
        ///     Deserialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="dictionary">Empty dictionary</param>
        /// <returns>The key list</returns>
        public override IDictionary DeserializeKeys(Type type, Type keyType, IDictionary dictionary)
        {
            var keyPath = _pathProvider.GetKeysPath(_basePath, DatabaseName, type, this);
            if (_fileHelper.FileExists(keyPath))
            {
                var pathLock = PathLock.GetLock(type.FullName);
                lock (pathLock)
                {
                    using (var keyFile = _fileHelper.GetReader(keyPath))
                    {
                        var count = keyFile.ReadInt32();
                        for (var x = 0; x < count; x++)
                        {
                            dictionary.Add(DatabaseSerializer.Deserialize(keyType, keyFile),
                                           keyFile.ReadInt32());
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
        public override void SerializeIndex<TKey, TIndex>(Type type, string indexName, Dictionary<TKey, TIndex> indexMap)
        {
            var indexPath = _pathProvider.GetIndexPath(_basePath, DatabaseName, type, this, indexName);
            var pathLock = PathLock.GetLock(type.FullName);
            lock(pathLock)
            {
                using (var indexFile = _fileHelper.GetWriter(indexPath))
                {
                    indexFile.Write(indexMap.Count);
                    foreach(var index in indexMap)
                    {
                        DatabaseSerializer.Serialize(index.Key, indexFile);
                        DatabaseSerializer.Serialize(index.Value, indexFile);
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
        public override void SerializeIndex<TKey, TIndex1, TIndex2>(Type type, string indexName, Dictionary<TKey, Tuple<TIndex1, TIndex2>> indexMap)
        {
            var indexPath = _pathProvider.GetIndexPath(_basePath, DatabaseName, type, this, indexName);
            var pathLock = PathLock.GetLock(type.FullName);
            lock (pathLock)
            {
                using (var indexFile = _fileHelper.GetWriter(indexPath))
                {
                    indexFile.Write(indexMap.Count);
                    foreach (var index in indexMap)
                    {
                        DatabaseSerializer.Serialize(index.Key, indexFile);
                        DatabaseSerializer.Serialize(index.Value.Item1, indexFile);
                        DatabaseSerializer.Serialize(index.Value.Item2, indexFile);
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
        public override Dictionary<TKey, TIndex> DeserializeIndex<TKey, TIndex>(Type type, string indexName)
        {
            var indexPath = _pathProvider.GetIndexPath(_basePath, DatabaseName, type, this, indexName);
            var dictionary = new Dictionary<TKey, TIndex>();
            if (_fileHelper.FileExists(indexPath))
            {
                var pathLock = PathLock.GetLock(type.FullName);
                lock (pathLock)
                {
                    using (var indexFile = _fileHelper.GetReader(indexPath))
                    {
                        var count = indexFile.ReadInt32();
                        for (var x = 0; x < count; x++)
                        {
                            dictionary.Add((TKey)DatabaseSerializer.Deserialize(typeof(TKey), indexFile),
                                           (TIndex)DatabaseSerializer.Deserialize(typeof(TIndex), indexFile));
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
        public override Dictionary<TKey, Tuple<TIndex1, TIndex2>> DeserializeIndex<TKey, TIndex1, TIndex2>(Type type, string indexName)
        {
            var indexPath = _pathProvider.GetIndexPath(_basePath, DatabaseName, type, this, indexName);
            var dictionary = new Dictionary<TKey, Tuple<TIndex1, TIndex2>>();
            if (_fileHelper.FileExists(indexPath))
            {
                var pathLock = PathLock.GetLock(type.FullName);
                lock (pathLock)
                {
                    using (var indexFile = _fileHelper.GetReader(indexPath))
                    {
                        var count = indexFile.ReadInt32();
                        for (var x = 0; x < count; x++)
                        {
                            dictionary.Add((TKey)DatabaseSerializer.Deserialize(typeof(TKey), indexFile),
                                Tuple.Create(           
                                (TIndex1)DatabaseSerializer.Deserialize(typeof(TIndex1), indexFile),
                                (TIndex2)DatabaseSerializer.Deserialize(typeof(TIndex2), indexFile)));
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
        public override void PublishTables(Dictionary<Type, ITableDefinition> tables)
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
                    var tableType = TableTypeResolver.ResolveTableType(fullTypeName);
                    if (tableType == null)
                    {
                        throw new SterlingTableNotFoundException(fullTypeName, DatabaseName);
                    }

                    GetTypeIndex(tableType.AssemblyQualifiedName);
                }
            }

            var pathLock = PathLock.GetLock(DatabaseName);
            lock (pathLock)
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
        public override void SerializeTypes()
        {
            var pathLock = PathLock.GetLock(TypeIndex.GetType().FullName);            
            lock (pathLock)
            {
                var typePath = _pathProvider.GetTypesPath(_basePath, DatabaseName, this);
                using (var typeFile = _fileHelper.GetWriter(typePath))
                {
                    typeFile.Write(TypeIndex.Count);
                    foreach (var type in TypeIndex)
                    {
                        typeFile.Write(type);
                    }
                }
            }
        }

        /// <summary>
        ///     Get the index for the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public override int GetTypeIndex(string type)
        {
            var pathLock = PathLock.GetLock(TypeIndex.GetType().FullName);
            lock(pathLock)
            {
                if (!TypeIndex.Contains(type))
                {
                    TypeIndex.Add(type);
                    _dirtyType = true;
                }
            }
            return TypeIndex.IndexOf(type);
        }

        /// <summary>
        ///     Get the type at an index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The type</returns>
        public override string GetTypeAtIndex(int index)
        {
            return TypeIndex[index];
        }
        
        /// <summary>
        ///     Save operation
        /// </summary>
        /// <param name="type">Type of the parent</param>
        /// <param name="keyIndex">Index for the key</param>
        /// <param name="bytes">The byte stream</param>
        public override void Save(Type type, int keyIndex, byte[] bytes)
        {            
            var instanceFolder = _pathProvider.GetInstanceFolder(_basePath, DatabaseName, type, this, keyIndex);
            _fileHelper.EnsureDirectory(instanceFolder);
            var instancePath = _pathProvider.GetInstancePath(_basePath, DatabaseName, type, this, keyIndex);
            
            // lock on this while saving, but remember that anyone else loading can now grab the
            // copy 
            lock (PathLock.GetLock(instancePath))
            {             
                using (
                    var instanceFile =
                        _fileHelper.GetWriter(instancePath))
                {
                    instanceFile.Write(bytes);
                    instanceFile.Flush();
                    instanceFile.Close();
                }                
            }

            if (!_dirtyType) return;

            _dirtyType = false;
            SerializeTypes();
        }   
            
        /// <summary>
        ///     Load from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        /// <returns>The byte stream</returns>
        public override BinaryReader Load(Type type, int keyIndex)
        {
            var instancePath = _pathProvider.GetInstancePath(_basePath, DatabaseName, type, this, keyIndex);
            
            // otherwise let's wait for it to be released and grab it from disk
            lock (PathLock.GetLock(instancePath))
            {
                return _fileHelper.FileExists(instancePath)
                           ? _fileHelper.GetReader(instancePath)
                           : new BinaryReader(new MemoryStream());
            }
        }

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        public override void Delete(Type type, int keyIndex)
        {
            var instancePath = _pathProvider.GetInstancePath(_basePath, DatabaseName, type, this, keyIndex);
            lock (PathLock.GetLock(instancePath))
            {
                if (_fileHelper.FileExists(instancePath))
                {
                    _fileHelper.Delete(instancePath);
                }
            }            
        }

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        public override void Truncate(Type type)
        {
            var folderPath = _pathProvider.GetTablePath(_basePath, DatabaseName, type, this);
            lock(PathLock.GetLock(type.FullName))
            {
                _fileHelper.Purge(folderPath);
            }
        }

        /// <summary>
        ///     Purge the database
        /// </summary>
        public override void Purge()
        {
            lock(PathLock.GetLock(DatabaseName))
            {
                _fileHelper.Purge(_pathProvider.GetDatabasePath(_basePath, DatabaseName, this));
            }
        }        
    }
}