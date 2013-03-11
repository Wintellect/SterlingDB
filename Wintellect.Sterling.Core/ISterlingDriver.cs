using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Core
{
    public interface ISterlingDriver
    {
        /// <summary>
        ///     Name of the database the driver is registered to
        /// </summary>
        string DatabaseInstanceName { get; set; }

        /// <summary>
        ///     Logger
        /// </summary>
        Action<SterlingLogLevel, string, Exception> Log { get; set; }

        /// <summary>
        ///     The registered serializer for the database
        /// </summary>
        ISterlingSerializer DatabaseSerializer { get; set; }

        /// <summary>
        ///     Serialize the keys
        /// </summary>
        /// <param name="type">Type of the parent table</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="keyMap">Key map</param>
        Task SerializeKeysAsync(Type type, Type keyType, IDictionary keyMap);

        /// <summary>
        ///     Deserialize keys without generics
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="keyType">Type of the key</param>
        /// <param name="template">The template</param>
        /// <returns>The keys without the template</returns>
        Task<IDictionary> DeserializeKeysAsync(Type type, Type keyType, IDictionary template);

        /// <summary>
        ///     Serialize a single index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>
        Task SerializeIndexAsync<TKey, TIndex>(Type type, string indexName, Dictionary<TKey, TIndex> indexMap);

        /// <summary>
        ///     Serialize a double index 
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex1">The type of the first index</typeparam>
        /// <typeparam name="TIndex2">The type of the second index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexMap">The index map</param>        
        Task SerializeIndexAsync<TKey, TIndex1, TIndex2>(Type type, string indexName, Dictionary<TKey, Tuple<TIndex1,TIndex2>> indexMap);

        /// <summary>
        ///     Deserialize a single index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>
        Task<Dictionary<TKey, TIndex>> DeserializeIndexAsync<TKey, TIndex>(Type type, string indexName);

        /// <summary>
        ///     Deserialize a double index
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TIndex1">The type of the first index</typeparam>
        /// <typeparam name="TIndex2">The type of the second index</typeparam>
        /// <param name="type">The type of the parent table</param>
        /// <param name="indexName">The name of the index</param>        
        /// <returns>The index map</returns>        
        Task<Dictionary<TKey, Tuple<TIndex1, TIndex2>>> DeserializeIndexAsync<TKey, TIndex1, TIndex2>(Type type, string indexName);

        /// <summary>
        ///     Publish the list of tables
        /// </summary>
        /// <param name="tables">The list of tables</param>
        void PublishTables(Dictionary<Type,ITableDefinition> tables, Func<string, Type> resolveType );

        /// <summary>
        ///     Serialize the type master
        /// </summary>
        Task SerializeTypesAsync();

        /// <summary>
        ///     Deserialize the type master
        /// </summary>
        /// <param name="types">The list of types</param>
        Task DeserializeTypesAsync(IList<string> types);

        /// <summary>
        ///     Get the type master
        /// </summary>
        /// <returns></returns>
        Task<IList<string>> GetTypesAsync();

        /// <summary>
        ///     Get the index for the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        Task<int> GetTypeIndexAsync(string type);

        /// <summary>
        ///     Get the type at an index
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The type</returns>
        Task<string> GetTypeAtIndexAsync(int index);

        /// <summary>
        ///     Save operation
        /// </summary>
        /// <param name="type">Type of the parent</param>
        /// <param name="keyIndex">Index for the key</param>
        /// <param name="bytes">The byte stream</param>
        Task SaveAsync(Type type, int keyIndex, byte[] bytes);

        /// <summary>
        ///     Load from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        /// <returns>The byte stream</returns>
        Task<BinaryReader> LoadAsync(Type type, int keyIndex);

        /// <summary>
        ///     Delete from the store
        /// </summary>
        /// <param name="type">The type of the parent</param>
        /// <param name="keyIndex">The index of the key</param>
        Task DeleteAsync(Type type, int keyIndex);

        /// <summary>
        ///     Truncate a type
        /// </summary>
        /// <param name="type">The type to truncate</param>
        Task TruncateAsync(Type type);

        /// <summary>
        ///     Purge the database
        /// </summary>
        Task PurgeAsync();        
    }
}
