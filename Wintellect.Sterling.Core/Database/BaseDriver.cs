using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    ///     Base driver
    /// </summary>
    public abstract class BaseDriver : ISterlingDriver
    {
        protected List<string> TypeIndex { get; private set; }

        protected BaseDriver()
        {
            TypeIndex = new List<string>();   
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="databaseName">Database</param>
        /// <param name="serializer">Serializer</param>
        /// <param name="log">Logging delegate</param>
        protected BaseDriver(string databaseName, ISterlingSerializer serializer, Action<SterlingLogLevel, string, Exception> log) : this()
        {            
            DatabaseName = databaseName;
            DatabaseSerializer = serializer;
            Log = log;
        }

        /// <summary>
        ///     Name of the database the driver is registered to
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        ///     Logger
        /// </summary>
        public Action<SterlingLogLevel, string, Exception> Log { get; set; }
        
        /// <summary>
        ///     The registered serializer for the database
        /// </summary>
        public ISterlingSerializer DatabaseSerializer { get; set; }

        /// <summary>
        ///     Serialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="keyMap">Key map</param>
        public abstract void SerializeKeys(Type type, Type keyType, IDictionary keyMap);

        /// <summary>
        ///     Deserialize keys without generics
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="template">The template</param>
        /// <returns>The keys without the template</returns>
        public abstract IDictionary DeserializeKeys(Type type, Type keyType, IDictionary template);        

        /// <summary>
        ///     Serialize a single index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>
        public abstract void SerializeIndex<TKey, TIndex>(Type type, string indexName, Dictionary<TKey, TIndex> indexMap);

        /// <summary>
        ///     Serialize a double index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex1">The type of the first index</typeparam>
        /// <typeparam name="TIndex2">The type of the second index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>        
        public abstract void SerializeIndex<TKey, TIndex1, TIndex2>(Type type, string indexName,
                                                                    Dictionary<TKey, Tuple<TIndex1, TIndex2>> indexMap);

        /// <summary>
        ///     Deserialize a single index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>
        public abstract Dictionary<TKey, TIndex> DeserializeIndex<TKey, TIndex>(Type type, string indexName);

        /// <summary>
        ///     Deserialize a double index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex1">The type of the first index</typeparam>
        /// <typeparam name="TIndex2">The type of the second index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>        
        public abstract Dictionary<TKey, Tuple<TIndex1, TIndex2>> DeserializeIndex<TKey, TIndex1, TIndex2>(Type type,
                                                                                                           string
                                                                                                               indexName);

        /// <summary>
        ///     Publish the list of tables
        /// </summary>
        /// <param name="tables">The list of tables</param>
        public abstract void PublishTables(Dictionary<Type, ITableDefinition> tables);

        /// <summary>
        ///     Serialize the type master
        /// </summary>
        public abstract void SerializeTypes();

        /// <summary>
        ///     Deserialize the type master
        /// </summary>
        /// <param name="types">The list of types</param>
        public void DeserializeTypes(IList<string> types)
        {
            TypeIndex = new List<string>(types);
        }

        /// <summary>
        ///     Get the type master
        /// </summary>
        /// <returns></returns>
        public IList<string> GetTypes()
        {
            return new List<string>(TypeIndex);
        }

        /// <summary>
        ///     Get the index for the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public virtual int GetTypeIndex(string type)
        {
            lock (((ICollection) TypeIndex).SyncRoot)
            {
                if (!TypeIndex.Contains(type))
                {
                    TypeIndex.Add(type);
                }
                return TypeIndex.IndexOf(type);
            }
        }

        /// <summary>
        ///     Get the type at an index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The type</returns>
        public virtual string GetTypeAtIndex(int index)
        {
            return TypeIndex[index];
        }

        /// <summary>
        ///     Save operation
        /// </summary>
        /// <param name="type">Type of the parent</param>
        /// <param name="keyIndex">Index for the key</param>
        /// <param name="bytes">The byte stream</param>
        public abstract void Save(Type type, int keyIndex, byte[] bytes);

        /// <summary>
        ///     Load from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        /// <returns>The byte stream</returns>
        public abstract BinaryReader Load(Type type, int keyIndex);

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        public abstract void Delete(Type type, int keyIndex);

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        public abstract void Truncate(Type type);

        /// <summary>
        ///     Purge the database
        /// </summary>
        public abstract void Purge();
    }
}