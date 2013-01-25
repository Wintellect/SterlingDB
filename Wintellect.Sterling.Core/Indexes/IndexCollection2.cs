using System;
using System.Collections.Generic;
using System.Linq;

namespace Wintellect.Sterling.Core.Indexes
{
    /// <summary>
    ///     Collection of keys for a given entity
    /// </summary>
    internal class IndexCollection<T, TIndex1, TIndex2, TKey> : IndexCollection<T, Tuple<TIndex1, TIndex2>, TKey>
        where T : class, new()
    {
        /// <summary>
        ///     Initialize the key collection
        /// </summary>
        /// <param name="name">Index name</param>
        /// <param name="driver">Sterling driver</param>
        /// <param name="indexer">How to resolve the index</param>
        /// <param name="resolver">The resolver for loading the object</param>
        public IndexCollection(string name, ISterlingDriver driver,
                               Func<T, Tuple<TIndex1, TIndex2>> indexer, Func<TKey, T> resolver) 
            : base(name, driver, indexer, resolver)
        {
            IsTuple = true;
        }

        /// <summary>
        ///     Deserialize the indexes
        /// </summary>
        protected override void DeserializeIndexes()
        {
            IndexList.Clear();

            foreach (var index in Driver.DeserializeIndex<TKey, TIndex1, TIndex2>(typeof(T), Name) ?? new Dictionary<TKey, Tuple<TIndex1, TIndex2>>())
            {
                IndexList.Add(new TableIndex<T, TIndex1, TIndex2, TKey>(index.Value.Item1, index.Value.Item2, index.Key, Resolver));
            }
        }

        /// <summary>
        ///     Serializes the key list
        /// </summary>
        protected override void SerializeIndexes()
        {
            var dictionary = IndexList.ToDictionary(item => item.Key, item => Tuple.Create(item.Index.Item1, item.Index.Item2));
            Driver.SerializeIndex(typeof(T), Name, dictionary);
        }
        

        /// <summary>
        ///     Add an index to the list
        /// </summary>
        /// <param name="index2">The second index</param>
        /// <param name="key">The related key</param>
        /// <param name="index1">The first index</param>
        public void AddIndex(object index1, object index2, object key)
        {
            var newIndex = new TableIndex<T, TIndex1, TIndex2, TKey>((TIndex1) index1, (TIndex2) index2, (TKey) key,
                                                                     Resolver);
            AddIndex(newIndex, key);
        }
    }
}