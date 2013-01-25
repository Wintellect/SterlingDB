using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Wintellect.Sterling.Core.Indexes
{
    /// <summary>
    ///     Collection of keys for a given entity
    /// </summary>
    internal class IndexCollection<T, TIndex, TKey> : IIndexCollection where T : class, new()
    {
        protected readonly ISterlingDriver Driver;
        protected Func<TKey, T> Resolver;
        private Func<T, TIndex> _indexer;
        protected string Name;
        
        /// <summary>
        ///     True if it is a tuple
        /// </summary>
        protected bool IsTuple { get; set; }

        /// <summary>
        ///     Set when keys change
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        ///     Initialize the key collection
        /// </summary>
        /// <param name="name">name of the index</param>
        /// <param name="driver">Sterling driver</param>
        /// <param name="indexer">How to resolve the index</param>
        /// <param name="resolver">The resolver for loading the object</param>
        public IndexCollection(string name, ISterlingDriver driver, Func<T,TIndex> indexer, Func<TKey,T> resolver)
        {        
            Driver = driver;
            _Setup(name, indexer, resolver);
        }        

        /// <summary>
        ///     Common constructor calls
        /// </summary>
        /// <param name="name"></param>
        /// <param name="indexer">How to resolve the index</param>
        /// <param name="resolver">The resolver for loading the object</param>
        private void _Setup(string name, Func<T, TIndex> indexer, Func<TKey, T> resolver)
        {
            Name = name;
            _indexer = indexer;
            Resolver = resolver;
            
            DeserializeIndexes();

            IsDirty = false;
        }

        /// <summary>
        ///     The list of indexes
        /// </summary>
        protected readonly List<TableIndex<T, TIndex, TKey>> IndexList = new List<TableIndex<T, TIndex, TKey>>();
        
        /// <summary>
        ///     Query the indexes
        /// </summary>
        public List<TableIndex<T, TIndex, TKey>> Query { get { return new List<TableIndex<T, TIndex, TKey>>(IndexList); } }

        /// <summary>
        ///     Deserialize the indexes
        /// </summary>
        protected virtual void DeserializeIndexes()
        {
            IndexList.Clear();

            foreach(var index in Driver.DeserializeIndex<TKey, TIndex>(typeof(T), Name) ?? new Dictionary<TKey, TIndex>())
            {
                IndexList.Add(new TableIndex<T, TIndex, TKey>(index.Value, index.Key, Resolver));
            }            
        }

        /// <summary>
        ///     Serializes the key list
        /// </summary>
        protected virtual void SerializeIndexes()
        {
            var dictionary = IndexList.ToDictionary(item => item.Key, item => item.Index);
            Driver.SerializeIndex(typeof(T), Name, dictionary);                      
        }
        
        /// <summary>
        ///     Serialize
        /// </summary>
        public void Flush()
        {
            lock (((ICollection)IndexList).SyncRoot)
            {
                if (IsDirty)
                {
                    SerializeIndexes();
                }
                IsDirty = false;
            }
        }             

        /// <summary>
        ///     Refresh the list
        /// </summary>
        public void Refresh()
        {
            lock (((ICollection)IndexList).SyncRoot)
            {
                if (IsDirty)
                {
                    SerializeIndexes();
                }
                DeserializeIndexes();
                IsDirty = false;
            }
        }

        /// <summary>
        ///     Truncate index
        /// </summary>
        public void Truncate()
        {
            IsDirty = false;
            Refresh();
        }
      
        /// <summary>
        ///     Add an index to the list
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The related key</param>
        public void AddIndex(object instance, object key)
        {
            var newIndex = new TableIndex<T, TIndex, TKey>(_indexer((T)instance), (TKey)key, Resolver);
            lock(((ICollection)IndexList).SyncRoot)
            {               
                if (!IndexList.Contains(newIndex))
                {
                    IndexList.Add(newIndex);
                }
                else
                {
                    IndexList[IndexList.IndexOf(newIndex)] = newIndex;
                }
            }
            IsDirty = true;
        }

        /// <summary>
        ///     Update the index
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The key</param>
        public void UpdateIndex(object instance, object key)
        {
            var index = (from i in IndexList where i.Key.Equals(key) select i).FirstOrDefault();

            if (index == null) return;

            index.Index = _indexer((T)instance);
            index.Refresh();
            IsDirty = true;
        }

        /// <summary>
        ///     Remove an index from the list
        /// </summary>
        /// <param name="key">The key</param>
        public void RemoveIndex(object key)
        {
            var index = (from i in IndexList where i.Key.Equals(key) select i).FirstOrDefault();

            if (index == null) return;
            
            lock(((ICollection)IndexList).SyncRoot)
            {
                if (!IndexList.Contains(index)) return;

                IndexList.Remove(index);
                IsDirty = true;
            }
        }        
    }
}
