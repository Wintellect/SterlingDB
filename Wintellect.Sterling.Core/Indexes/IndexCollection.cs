using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            DeserializeIndexesAsync().Wait();   // need to fix this

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
        protected virtual Task DeserializeIndexesAsync()
        {
            return Task.Factory.StartNew( () =>
            {
                IndexList.Clear();

                var task = Driver.DeserializeIndexAsync<TKey, TIndex>( typeof( T ), Name );

                foreach ( var index in task.Result ?? new Dictionary<TKey, TIndex>() )
                {
                    IndexList.Add( new TableIndex<T, TIndex, TKey>( index.Value, index.Key, Resolver ) );
                }
            }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Serializes the key list
        /// </summary>
        protected virtual Task SerializeIndexesAsync()
        {
            return Task.Factory.StartNew( () =>
                {
                    var dictionary = IndexList.ToDictionary( item => item.Key, item => item.Index );
                    
                    Driver.SerializeIndexAsync( typeof( T ), Name, dictionary );
                }, TaskCreationOptions.AttachedToParent );
        }
        
        /// <summary>
        ///     Serialize
        /// </summary>
        public Task FlushAsync()
        {
            return Task.Factory.StartNew( () =>
            {
                lock ( ( (ICollection) IndexList ).SyncRoot )
                {
                    if ( IsDirty )
                    {
                        SerializeIndexesAsync();
                    }
                    IsDirty = false;
                }
            }, TaskCreationOptions.AttachedToParent );
        }             

        /// <summary>
        ///     Refresh the list
        /// </summary>
        public Task RefreshAsync()
        {
            return Task.Factory.StartNew( () =>
                {
                    lock ( ( (ICollection) IndexList ).SyncRoot )
                    {
                        if ( IsDirty )
                        {
                            SerializeIndexesAsync().Wait();
                        }
                        
                        DeserializeIndexesAsync().Wait();
                        
                        IsDirty = false;
                    }
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Truncate index
        /// </summary>
        public Task TruncateAsync()
        {
            return Task.Factory.StartNew( () =>
            {
                IsDirty = false;
                RefreshAsync();
            }, TaskCreationOptions.AttachedToParent );
        }
      
        /// <summary>
        ///     Add an index to the list
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The related key</param>
        public Task AddIndexAsync(object instance, object key)
        {
            return Task.Factory.StartNew( () =>
                {
                    var newIndex = new TableIndex<T, TIndex, TKey>( _indexer( (T) instance ), (TKey) key, Resolver );
                    lock ( ( (ICollection) IndexList ).SyncRoot )
                    {
                        if ( !IndexList.Contains( newIndex ) )
                        {
                            IndexList.Add( newIndex );
                        }
                        else
                        {
                            IndexList[ IndexList.IndexOf( newIndex ) ] = newIndex;
                        }
                    }
                    IsDirty = true;
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Update the index
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The key</param>
        public Task UpdateIndexAsync(object instance, object key)
        {
            return Task.Factory.StartNew( () =>
                {
                    var index = ( from i in IndexList where i.Key.Equals( key ) select i ).FirstOrDefault();

                    if ( index == null ) return;

                    index.Index = _indexer( (T) instance );
                    index.Refresh();
                    IsDirty = true;
                }, TaskCreationOptions.AttachedToParent );
        }

        /// <summary>
        ///     Remove an index from the list
        /// </summary>
        /// <param name="key">The key</param>
        public Task RemoveIndexAsync(object key)
        {
            return Task.Factory.StartNew( () =>
                {
                    var index = ( from i in IndexList where i.Key.Equals( key ) select i ).FirstOrDefault();

                    if ( index == null ) return;

                    lock ( ( (ICollection) IndexList ).SyncRoot )
                    {
                        if ( !IndexList.Contains( index ) ) return;

                        IndexList.Remove( index );
                        IsDirty = true;
                    }
                }, TaskCreationOptions.AttachedToParent );
        }        
    }
}
