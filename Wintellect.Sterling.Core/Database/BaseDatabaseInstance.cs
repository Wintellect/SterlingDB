using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wintellect.Sterling.Core.Events;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Core.Indexes;
using Wintellect.Sterling.Core.Keys;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    ///     Base class for a sterling database instance
    /// </summary>
    public abstract class BaseDatabaseInstance : ISterlingDatabaseInstance
    {
        public ISterlingDriver Driver { get; private set; }

        /// <summary>
        ///     Master database locks
        /// </summary>
        private readonly AsyncLock _lock = new AsyncLock();
        private long _taskCount = 0;
        private SerializationHelper _serializationHelper;

        public SerializationHelper Helper
        {
            get
            {
                return _serializationHelper ?? (_serializationHelper = new SerializationHelper(this, Serializer, this.Database.LogManager,
                                                                    s => Driver.GetTypeIndexAsync(s).Result,
                                                                    i => Driver.GetTypeAtIndexAsync(i).Result));
            }

        }

        /// <summary>
        ///     List of triggers
        /// </summary>
        private readonly Dictionary<Type, List<ISterlingTrigger>> _triggers =
            new Dictionary<Type, List<ISterlingTrigger>>();

        /// <summary>
        ///     The table definitions
        /// </summary>
        internal readonly Dictionary<Type, ITableDefinition> TableDefinitions = new Dictionary<Type, ITableDefinition>();

        /// <summary>
        ///     Serializer
        /// </summary>
        internal ISterlingSerializer Serializer { get; set; }

        internal SterlingDatabase Database { get; set; }

        /// <summary>
        ///     The base database instance
        /// </summary>
        protected BaseDatabaseInstance()
        {
        }

        public void Unload()
        {
            FlushAsync().Wait();
        }

        private async Task<IDisposable> LockAsync()
        {
            return await _lock.LockAsync().ConfigureAwait( false );
        }

        /// <summary>
        ///     Register a trigger
        /// </summary>
        /// <param name="trigger">The trigger</param>
        public void RegisterTrigger<T, TKey>(BaseSterlingTrigger<T, TKey> trigger) where T : class, new()
        {
            using ( LockAsync().Result )
            {
                if ( !_triggers.ContainsKey( typeof( T ) ) )
                {
                    _triggers.Add( typeof( T ), new List<ISterlingTrigger>() );
                }

                _triggers[ typeof( T ) ].Add( trigger );
            }
        }

        /// <summary>
        /// The byte stream interceptor list. 
        /// </summary>
        private readonly List<BaseSterlingByteInterceptor> _byteInterceptorList =
            new List<BaseSterlingByteInterceptor>();

        /// <summary>
        /// Registers the BaseSterlingByteInterceptor
        /// </summary>
        /// <typeparam name="T">The type of the interceptor</typeparam>
        public void RegisterInterceptor<T>() where T : BaseSterlingByteInterceptor, new()
        {
            using ( LockAsync().Result )
            {
                _byteInterceptorList.Add( (T) Activator.CreateInstance( typeof ( T ) ) );
            }
        }

        public void UnRegisterInterceptor<T>() where T : BaseSterlingByteInterceptor, new()
        {
            using ( LockAsync().Result )
            {
                var interceptor = ( from i
                                        in _byteInterceptorList
                                    where i.GetType().Equals( typeof ( T ) )
                                    select i ).FirstOrDefault();

                if ( interceptor != null )
                {
                    _byteInterceptorList.Remove( interceptor );
                }
            }
        }

        /// <summary>
        /// Clears the _byteInterceptorList object
        /// </summary>
        public void UnRegisterInterceptors()
        {
            using ( LockAsync().Result )
            {
                if ( _byteInterceptorList != null )
                {
                    _byteInterceptorList.Clear();
                }
            }
        }


        /// <summary>
        ///     Unregister the trigger
        /// </summary>
        /// <param name="trigger">The trigger</param>
        public void UnregisterTrigger<T, TKey>(BaseSterlingTrigger<T, TKey> trigger) where T : class, new()
        {
            using ( LockAsync().Result )
            {
                _triggers[ typeof( T ) ].Remove( trigger );
            }
        }

        /// <summary>
        ///     Fire the triggers for a type
        /// </summary>
        /// <param name="type">The target type</param>
        private IEnumerable<ISterlingTrigger> _TriggerList(Type type)
        {
            List<ISterlingTrigger> triggers = null;

            _triggers.TryGetValue( type, out triggers );

            return triggers ?? new List<ISterlingTrigger>();
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name
        {
            get { return this.Driver.DatabaseInstanceName; }
        }

        /// <summary>
        ///     The type dictating which objects should be ignored
        /// </summary>
        public virtual Type IgnoreAttribute { get { return typeof(SterlingIgnoreAttribute); } }

        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected abstract List<ITableDefinition> RegisterTables();

        /// <summary>
        ///     Register any type resolvers.
        /// </summary>
        internal virtual void RegisterTypeResolvers()
        {
        }

        /// <summary>
        ///     Registers any property converters.
        /// </summary>
        internal virtual void RegisterPropertyConverters()
        {
        }

        /// <summary>
        /// Register a class responsible for type resolution.
        /// </summary>
        /// <param name="typeInterceptor"></param>
        protected void RegisterTypeResolver(ISterlingTypeResolver typeInterceptor)
        {
            this.Database.RegisterTypeResolver(typeInterceptor);
        }

        private readonly Dictionary<Type, ISterlingPropertyConverter> _propertyConverters = new Dictionary<Type, ISterlingPropertyConverter>();

        /// <summary>
        ///     Registers a property converter.
        /// </summary>
        /// <param name="propertyConverter">The property converter</param>
        protected void RegisterPropertyConverter(ISterlingPropertyConverter propertyConverter)
        {
            _propertyConverters.Add(propertyConverter.IsConverterFor(), propertyConverter);
        }

        /// <summary>
        ///     Gets the property converter for the given type, or returns null if none is found.
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="propertyConverter">The property converter</param>
        /// <returns>True if there is a registered property converter.</returns>
        public bool TryGetPropertyConverter(Type type, out ISterlingPropertyConverter propertyConverter)
        {
            return _propertyConverters.TryGetValue(type, out propertyConverter);
        }

        /// <summary>
        ///     Returns a table definition 
        /// </summary>
        /// <typeparam name="T">The type of the table</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="keyFunction">The key mapping function</param>
        /// <returns>The table definition</returns>
        public ITableDefinition CreateTableDefinition<T, TKey>(Func<T, TKey> keyFunction) where T : class, new()
        {
            return new TableDefinition<T, TKey>(Driver,
                                                ( key => _Load<T>( typeof( T ), key, new CycleCache() ).Result ),
                                                keyFunction);
        }

        /// <summary>
        ///     Get the list of table definitions
        /// </summary>
        /// <returns>The list of table definitions</returns>
        public IEnumerable<ITableDefinition> GetTableDefinitions()
        {
            return new List<ITableDefinition>(TableDefinitions.Values);
        }

        /// <summary>
        ///     Register a new table definition
        /// </summary>
        /// <param name="tableDefinition">The new table definition</param>
        public void RegisterTableDefinition(ITableDefinition tableDefinition)
        {
            using ( LockAsync().Result )
            {
                if ( !TableDefinitions.ContainsKey( tableDefinition.TableType ) )
                {
                    TableDefinitions.Add( tableDefinition.TableType, tableDefinition );
                }
            }
        }

        /// <summary>
        ///     Call to publish tables 
        /// </summary>
        internal void PublishTables(ISterlingDriver driver)
        {
            using ( LockAsync().Result )
            {
                Driver = driver;

                foreach ( var table in RegisterTables() )
                {
                    if ( TableDefinitions.ContainsKey( table.TableType ) )
                    {
                        throw new SterlingDuplicateTypeException( table.TableType, Name );
                    }

                    TableDefinitions.Add( table.TableType, table );
                }

                Driver.PublishTables( TableDefinitions, this.Database.TypeResolver.ResolveTableType );
            }
        }        

        /// <summary>
        ///     True if it is registered with the sterling engine
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>True if it can be persisted</returns>
        public bool IsRegistered<T>(T instance) where T : class
        {
            return IsRegistered(typeof (T));
        }

        /// <summary>
        ///     Non-generic registration check
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>True if it is registered</returns>
        public bool IsRegistered(Type type)
        {
            return TableDefinitions.ContainsKey(type);
        }

        /// <summary>
        ///     Get the key for an object
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object GetKey(object instance)
        {
            if (!IsRegistered(instance.GetType()))
            {
                throw new SterlingTableNotFoundException(instance.GetType(), Name);
            }

            return TableDefinitions[instance.GetType()].FetchKeyFromInstance(instance);
        }

        /// <summary>
        ///     Get the key for an object
        /// </summary>
        /// <param name="table">The instance type</param>
        /// <returns>The key type</returns>
        public Type GetKeyType(Type table)
        {
            if (!IsRegistered(table))
            {
                throw new SterlingTableNotFoundException(table, Name);
            }

            return TableDefinitions[table].KeyType;
        }

        /// <summary>
        ///     Query (keys only)
        /// </summary>
        /// <typeparam name="T">The type to query</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <returns>The list of keys to query</returns>
        public List<TableKey<T, TKey>> Query<T, TKey>() where T : class, new()
        {
            if (!IsRegistered(typeof (T)))
            {
                throw new SterlingTableNotFoundException(typeof (T), Name);
            }

            return
                new List<TableKey<T, TKey>>(
                    ((TableDefinition<T, TKey>) TableDefinitions[typeof (T)]).KeyList.Query);
        }

        /// <summary>
        ///     Query an index
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <typeparam name="TIndex">The index type</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <param name="indexName">The name of the index</param>
        /// <returns>The indexed items</returns>
        public List<TableIndex<T, TIndex, TKey>> Query<T, TIndex, TKey>(string indexName) where T : class, new()
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException("indexName");
            }

            if (!IsRegistered(typeof (T)))
            {
                throw new SterlingTableNotFoundException(typeof (T), Name);
            }

            var tableDef = (TableDefinition<T, TKey>) TableDefinitions[typeof (T)];

            if (!tableDef.Indexes.ContainsKey(indexName))
            {
                throw new SterlingIndexNotFoundException(indexName, typeof (T));
            }

            var collection = tableDef.Indexes[indexName] as IndexCollection<T, TIndex, TKey>;

            if (collection == null)
            {
                throw new SterlingIndexNotFoundException(indexName, typeof (T));
            }

            return new List<TableIndex<T, TIndex, TKey>>(collection.Query);
        }

        /// <summary>
        ///     Query an index
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <typeparam name="TIndex1">The first index type</typeparam>
        /// <typeparam name="TIndex2">The second index type</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <param name="indexName">The name of the index</param>
        /// <returns>The indexed items</returns>
        public List<TableIndex<T, Tuple<TIndex1, TIndex2>, TKey>> Query<T, TIndex1, TIndex2, TKey>(string indexName)
            where T : class, new()
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException("indexName");
            }

            if (!IsRegistered(typeof (T)))
            {
                throw new SterlingTableNotFoundException(typeof (T), Name);
            }

            var tableDef = (TableDefinition<T, TKey>) TableDefinitions[typeof (T)];

            if (!tableDef.Indexes.ContainsKey(indexName))
            {
                throw new SterlingIndexNotFoundException(indexName, typeof (T));
            }

            var collection = tableDef.Indexes[indexName] as IndexCollection<T, TIndex1, TIndex2, TKey>;

            if (collection == null)
            {
                throw new SterlingIndexNotFoundException(indexName, typeof (T));
            }

            return new List<TableIndex<T, Tuple<TIndex1, TIndex2>, TKey>>(collection.Query);
        }

        /// <summary>
        ///     Save it
        /// </summary>
        /// <typeparam name="T">The instance type</typeparam>
        /// <typeparam name="TKey">Save it</typeparam>
        /// <param name="instance">The instance</param>
        public async Task<TKey> SaveAsync<T, TKey>( T instance ) where T : class, new()
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                return await _Save<TKey>( typeof( T ), typeof( T ), instance, new CycleCache() ).ConfigureAwait( false );
            }
        }

        /// <summary>
        ///     Save an instance against a base class table definition
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <typeparam name="TKey">Save it</typeparam>
        /// <param name="instance">An instance or sub-class of the table type</param>
        /// <returns></returns>
        public async Task<TKey> SaveAsAsync<T, TKey>( T instance ) where T : class, new()
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                return await _Save<TKey>( instance.GetType(), typeof( T ), instance, new CycleCache() ).ConfigureAwait( false );
            }
        }

        /// <summary>
        ///     Save an instance against a base class table definition
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <param name="instance">An instance or sub-class of the table type</param>
        /// <returns></returns>
        public Task<object> SaveAsAsync<T>(T instance) where T : class, new()
        {
            var tableType = typeof(T);
            return SaveAsync(instance.GetType(), tableType, instance, new CycleCache());
        }

        /// <summary>
        ///     Save against a base class when key is not known
        /// </summary>
        /// <param name="tableType"></param>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public Task<object> SaveAsAsync(Type tableType, object instance)
        {
            if (!this.Database.Engine.PlatformAdapter.IsSubclassOf( instance.GetType(), tableType) || instance.GetType() != tableType)
            {
                throw new SterlingException(string.Format("{0} is not of type {1}", instance.GetType().Name, tableType.Name));
            }

            return SaveAsync(tableType, instance);
        }

        /// <summary>
        ///     Entry point for save
        /// </summary>
        /// <param name="type">Type to save</param>
        /// <param name="instance">Instance</param>
        /// <returns>The key saved</returns>
        public Task<object> SaveAsync(Type type, object instance)
        {
            return SaveAsync(type, type, instance, new CycleCache());
        }

        /// <summary>
        ///     Save when key is not known
        /// </summary>
        /// <param name="actualType">The type of instance to save</param>
        /// <param name="tableType">The table type to save to</param>
        /// <param name="instance">The instance</param>
        /// <param name="cache">Cycle cache</param>
        /// <returns>The key</returns>
        public async Task<object> SaveAsync( Type actualType, Type tableType, object instance, CycleCache cache )
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                return await _Save<object>( actualType, tableType, instance, cache ).ConfigureAwait( false );
            }
        }

        /// <summary>
        ///     Save when key is not known
        /// </summary>
        /// <typeparam name="T">The type of the instance</typeparam>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public Task<object> SaveAsync<T>(T instance) where T : class, new()
        {
            return SaveAsync(typeof (T), instance);
        }

        internal async Task<TKey> _Save<TKey>( Type actualType, Type tableType, object instance, CycleCache cache )
        {
            ITableDefinition tableDef = null;

            if ( !TableDefinitions.ContainsKey( tableType ) )
            {
                throw new SterlingTableNotFoundException( instance.GetType(), Name );
            }

            tableDef = TableDefinitions[ tableType ];

            if ( !tableDef.IsDirty( instance ) )
            {
                return (TKey) tableDef.FetchKeyFromInstance( instance );
            }

            foreach ( var trigger in _TriggerList( tableType ).Where( trigger => !trigger.BeforeSave( actualType, instance ) ) )
            {
                throw new SterlingTriggerException( Exceptions.Exceptions.BaseDatabaseInstance_Save_Save_suppressed_by_trigger, trigger.GetType() );
            }

            var key = (TKey) tableDef.FetchKeyFromInstance( instance );

            int keyIndex;

            if ( cache.Check( instance ) )
            {
                return key;
            } 

            cache.Add( tableType, instance, key );

            keyIndex = await tableDef.Keys.AddKeyAsync( key ).ConfigureAwait( false );

            var memStream = new MemoryStream();

            try
            {
                using ( var bw = new BinaryWriter( memStream ) )
                {
                    Helper.Save( actualType, instance, bw, cache, true );

                    bw.Flush();

                    if ( _byteInterceptorList.Count > 0 )
                    {
                        var bytes = memStream.ToArray();

                        bytes = _byteInterceptorList.Aggregate( bytes,
                                                                ( current, byteInterceptor ) =>
                                                                byteInterceptor.Save( current ) );

                        memStream = new MemoryStream( bytes );
                    }

                    memStream.Seek( 0, SeekOrigin.Begin );

                    await Driver.SaveAsync( tableType, keyIndex, memStream.ToArray() ).ConfigureAwait( false );
                }
            }
            finally
            {
                memStream.Flush();
                memStream.Dispose();
            }

            // update the indexes
            foreach ( var index in tableDef.Indexes.Values )
            {
                await index.AddIndexAsync( instance, key ).ConfigureAwait( false );
            }

            // call post-save triggers
            foreach ( var trigger in _TriggerList( tableType ) )
            {
                trigger.AfterSave( actualType, instance );
            }

            _RaiseOperation( SterlingOperation.Save, tableType, key );

            return key;
        }

        /// <summary>
        ///     Flush all keys and indexes to storage
        /// </summary>
        public async Task FlushAsync()
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                Interlocked.Increment( ref _taskCount );

                try
                {
                    foreach ( var def in TableDefinitions.Values )
                    {
                        await def.Keys.FlushAsync().ConfigureAwait( false );

                        foreach ( var idx in def.Indexes.Values )
                        {
                            await idx.FlushAsync().ConfigureAwait( false );
                        }
                    }

                    _RaiseOperation( SterlingOperation.Flush, GetType(), Name );
                }
                finally
                {
                    Interlocked.Decrement( ref _taskCount );
                }
            }
        }

        /// <summary>
        ///     Load it 
        /// </summary>
        /// <typeparam name="T">The type to load</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <param name="key">The value of the key</param>
        /// <returns>The instance</returns>
        public Task<T> LoadAsync<T, TKey>(TKey key) where T : class, new()
        {
            return LoadAsync<T>( (object) key);
        }

        /// <summary>
        ///     Load it (key type not typed)
        /// </summary>
        /// <typeparam name="T">The type to load</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The instance</returns>
        public async Task<T> LoadAsync<T>( object key ) where T : class, new()
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                return await _Load<T>( typeof( T ), key, new CycleCache() ).ConfigureAwait( false );
            }
        }

        /// <summary>
        ///     Load entry point with new cycle cache
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <param name="key">The key</param>
        /// <returns>The object</returns>
        public Task<object> LoadAsync(Type type, object key)
        {
            return LoadAsync(type, key, new CycleCache());
        }

        /// <summary>
        ///     Load it without knowledge of the key type
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <param name="key">The key</param>
        /// <param name="cache">Cache queue</param>
        /// <returns>The instance</returns>
        public async Task<object> LoadAsync( Type type, object key, CycleCache cache )
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                return await _Load<object>( type, key, cache ).ConfigureAwait( false );
            }
        }

        internal async Task<TResult> _Load<TResult>( Type type, object key, CycleCache cache ) where TResult : new()
        {
            Interlocked.Increment( ref _taskCount );

            try
            {
                var newType = type;
                var keyIndex = -1;

                ITableDefinition tableDef = null;

                tableDef = TableDefinitions.SingleOrDefault( pair => pair.Key == type ).Value;

                if ( tableDef == null )
                {
                    foreach ( var subDef in TableDefinitions.Where( pair => this.Database.Engine.PlatformAdapter.IsAssignableFrom( type, pair.Key ) )
                                                            .Select( pair => pair.Value ) )
                    {
                        keyIndex = await subDef.Keys.GetIndexForKeyAsync( key ).ConfigureAwait( false );

                        if ( keyIndex < 0 ) continue;

                        newType = subDef.TableType;
                        tableDef = subDef;
                        break;
                    }

                    if ( tableDef == null )
                    {
                        throw new SterlingTableNotFoundException( type, Name );
                    }
                }
                else
                {
                    keyIndex = await tableDef.Keys.GetIndexForKeyAsync( key ).ConfigureAwait( false );
                }

                if ( keyIndex < 0 )
                {
                    return default( TResult );
                }

                var obj = (TResult) cache.CheckKey( newType, key );

                if ( obj != null )
                {
                    return obj;
                }

                BinaryReader br = null;
                MemoryStream memStream = null;

                try
                {
                    br = await Driver.LoadAsync( newType, keyIndex ).ConfigureAwait( false );

                    if ( _byteInterceptorList.Count > 0 )
                    {
                        var bytes = br.ReadBytes( (int) br.BaseStream.Length );

                        bytes = _byteInterceptorList.ToArray().Reverse().Aggregate( bytes,
                                                                                    ( current, byteInterceptor ) =>
                                                                                    byteInterceptor.Load( current ) );

                        memStream = new MemoryStream( bytes );

                        br.Dispose();

                        br = new BinaryReader( memStream );
                    }

                    obj = (TResult) Helper.Load( newType, key, br, cache );
                }
                finally
                {
                    if ( br != null )
                    {
                        br.Dispose();
                    }

                    if ( memStream != null )
                    {
                        memStream.Flush();
                        memStream.Dispose();
                    }
                }

                _RaiseOperation( SterlingOperation.Load, newType, key );

                return obj;
            }
            finally
            {
                Interlocked.Decrement( ref _taskCount );
            }
        }

        /// <summary>
        ///     Delete it 
        /// </summary>
        /// <typeparam name="T">The type to delete</typeparam>
        /// <param name="instance">The instance</param>
        public Task DeleteAsync<T>(T instance) where T : class
        {
            return DeleteAsync(typeof (T), TableDefinitions[typeof (T)].FetchKeyFromInstance(instance));
        }

        /// <summary>
        ///     Delete it (non-generic)
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="key">The key</param>
        public async Task DeleteAsync(Type type, object key)
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                ITableDefinition tableDef = null;

                if ( !TableDefinitions.ContainsKey( type ) )
                {
                    throw new SterlingTableNotFoundException( type, Name );
                }

                tableDef = TableDefinitions[ type ];

                // call any before save triggers 
                foreach ( var trigger in _TriggerList( type ).Where( trigger => !trigger.BeforeDelete( type, key ) ) )
                {
                    throw new SterlingTriggerException( string.Format( Exceptions.Exceptions.BaseDatabaseInstance_Delete_Delete_failed_for_type, type ), trigger.GetType() );
                }

                var keyEntry = await tableDef.Keys.GetIndexForKeyAsync( key ).ConfigureAwait( false );

                await Driver.DeleteAsync( type, keyEntry ).ConfigureAwait( false );

                await tableDef.Keys.RemoveKeyAsync( key ).ConfigureAwait( false );

                foreach ( var index in tableDef.Indexes.Values )
                {
                    await index.RemoveIndexAsync( key ).ConfigureAwait( false );
                }

                _RaiseOperation( SterlingOperation.Delete, type, key );
            }
        }

        /// <summary>
        ///     Truncate all records for a type
        /// </summary>
        /// <param name="type">The type</param>
        public async Task TruncateAsync(Type type)
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                if ( Interlocked.Read( ref _taskCount ) > 1 )
                {
                    throw new SterlingException(
                        Exceptions.Exceptions.BaseDatabaseInstance_Truncate_Cannot_truncate_when_background_operations );
                }

                Interlocked.Increment( ref _taskCount );

                try
                {
                    await Driver.TruncateAsync( type ).ConfigureAwait( false );

                    await TableDefinitions[ type ].Keys.TruncateAsync().ConfigureAwait( false );

                    foreach ( var index in TableDefinitions[ type ].Indexes.Values )
                    {
                        await index.TruncateAsync().ConfigureAwait( false );
                    }

                    _RaiseOperation( SterlingOperation.Truncate, type, null );
                }
                finally
                {
                    Interlocked.Decrement( ref _taskCount );
                }
            }
        }

        /// <summary>
        ///     Purge the entire database - wipe it clean!
        /// </summary>
        public async Task PurgeAsync()
        {
            using ( await LockAsync().ConfigureAwait( false ) )
            {
                if ( Interlocked.Read( ref _taskCount ) > 1 )
                {
                    throw new SterlingException(
                        Exceptions.Exceptions.BaseDatabaseInstance_Cannot_purge_when_background_operations );
                }

                Interlocked.Increment( ref _taskCount );

                try
                {
                    await Driver.PurgeAsync().ConfigureAwait( false );

                    // clear key lists from memory
                    foreach ( var table in TableDefinitions.Keys )
                    {
                        await TableDefinitions[ table ].Keys.TruncateAsync().ConfigureAwait( false );

                        foreach ( var index in TableDefinitions[ table ].Indexes.Values )
                        {
                            await index.TruncateAsync().ConfigureAwait( false );
                        }
                    }

                    _RaiseOperation( SterlingOperation.Purge, GetType(), Name );
                }
                finally
                {
                    Interlocked.Decrement( ref _taskCount );
                }
            }
        }

        /// <summary>
        ///     Refresh indexes and keys from disk
        /// </summary>
        public async Task RefreshAsync()
        {
            foreach ( var table in TableDefinitions )
            {
                await table.Value.RefreshAsync().ConfigureAwait( false );
            }
        }

        /// <summary>
        ///     Raise an operation
        /// </summary>
        /// <remarks>
        ///     Only send if access to the UI thread is available
        /// </remarks>
        /// <param name="operation">The operation</param>
        /// <param name="targetType">Target type</param>
        /// <param name="key">Key</param>
        private void _RaiseOperation(SterlingOperation operation, Type targetType, object key)
        {
            var handler = SterlingOperationPerformed;

            if (handler == null) return;

            handler(this, new SterlingOperationArgs(operation, targetType, key));
        }

        public event EventHandler<SterlingOperationArgs> SterlingOperationPerformed;
    }
}