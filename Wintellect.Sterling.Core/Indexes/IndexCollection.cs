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
        protected readonly AsyncLock _lock = new AsyncLock();
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

            DeserializeIndexesAsync().Wait();

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
        protected virtual async Task DeserializeIndexesAsync()
        {
            IndexList.Clear();

            var result = await Driver.DeserializeIndexAsync<TKey, TIndex>( typeof( T ), Name ).ConfigureAwait( false );

            foreach ( var index in result ?? new Dictionary<TKey, TIndex>() )
            {
                IndexList.Add( new TableIndex<T, TIndex, TKey>( index.Value, index.Key, Resolver ) );
            }
        }

        /// <summary>
        ///     Serializes the key list
        /// </summary>
        protected virtual async Task SerializeIndexesAsync()
        {
            var dictionary = IndexList.ToDictionary( item => item.Key, item => item.Index );

            await Driver.SerializeIndexAsync( typeof( T ), Name, dictionary ).ConfigureAwait( false );
        }
        
        /// <summary>
        ///     Serialize
        /// </summary>
        public async Task FlushAsync()
        {
            using ( await _lock.LockAsync().ConfigureAwait( false ) )
            {
                if ( IsDirty )
                {
                    await SerializeIndexesAsync().ConfigureAwait( false );
                }

                IsDirty = false;
            }
        }             

        /// <summary>
        ///     Refresh the list
        /// </summary>
        public async Task RefreshAsync()
        {
            using ( await _lock.LockAsync().ConfigureAwait( false ) )
            {
                if ( IsDirty )
                {
                    await SerializeIndexesAsync().ConfigureAwait( false );
                }

                await DeserializeIndexesAsync().ConfigureAwait( false );

                IsDirty = false;
            }
        }

        /// <summary>
        ///     Truncate index
        /// </summary>
        public async Task TruncateAsync()
        {
            IsDirty = false;

            await RefreshAsync().ConfigureAwait( false );
        }
      
        /// <summary>
        ///     Add an index to the list
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The related key</param>
        public async Task AddIndexAsync(object instance, object key)
        {
            var newIndex = new TableIndex<T, TIndex, TKey>( _indexer( (T) instance ), (TKey) key, Resolver );

            using ( await _lock.LockAsync().ConfigureAwait( false ) )
            {
                if ( !IndexList.Contains( newIndex ) )
                {
                    IndexList.Add( newIndex );
                }
                else
                {
                    IndexList[ IndexList.IndexOf( newIndex ) ] = newIndex;
                }

                IsDirty = true;
            }
        }

        /// <summary>
        ///     Update the index
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The key</param>
        public async Task UpdateIndexAsync(object instance, object key)
        {
            var index = ( from i in IndexList where i.Key.Equals( key ) select i ).FirstOrDefault();

            if ( index == null ) return;

            index.Index = _indexer( (T) instance );

            index.Refresh();

            IsDirty = true;
        }

        /// <summary>
        ///     Remove an index from the list
        /// </summary>
        /// <param name="key">The key</param>
        public async Task RemoveIndexAsync(object key)
        {
            var index = ( from i in IndexList where i.Key.Equals( key ) select i ).FirstOrDefault();

            if ( index == null ) return;

            using ( await _lock.LockAsync().ConfigureAwait( false ) )
            {
                if ( !IndexList.Contains( index ) ) return;

                IndexList.Remove( index );

                IsDirty = true;
            }
        }        
    }
}
