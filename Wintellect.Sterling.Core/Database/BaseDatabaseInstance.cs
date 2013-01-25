using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
        private static readonly Dictionary<Type, object> _locks = new Dictionary<Type, object>();

        /// <summary>
        ///     Workers to track/flush
        /// </summary>
        private readonly List<PendingOperation> _workers = new List<PendingOperation>();

        private SerializationHelper _serializationHelper; 

        public SerializationHelper Helper
        {
            get
            {
                return _serializationHelper ?? (_serializationHelper = new SerializationHelper(this, Serializer, SterlingFactory.GetLogger(),
                                                                    s => Driver.GetTypeIndex(s),
                                                                    i => Driver.GetTypeAtIndex(i)));
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

        /// <summary>
        ///     Called when this should be deactivated
        /// </summary>
        internal static void Deactivate()
        {
            _locks.Clear();
        }

        /// <summary>
        ///     The base database instance
        /// </summary>
        protected BaseDatabaseInstance()
        {
            var registered = false;

            lock (((ICollection) _locks).SyncRoot)
            {
                if (!_locks.ContainsKey(GetType()))
                {
                    _locks.Add(GetType(), new object());
                }
                else
                {
                    registered = true;
                }
            }

            if (registered)
            {
                throw new SterlingDuplicateDatabaseException(this);
            }
        }

        public void Unload()
        {
            Flush();
        }

        /// <summary>
        ///     Must return an object for synchronization
        /// </summary>
        public object Lock
        {
            get { return _locks[GetType()]; }
        }

        /// <summary>
        ///     Register a trigger
        /// </summary>
        /// <param name="trigger">The trigger</param>
        public void RegisterTrigger<T, TKey>(BaseSterlingTrigger<T, TKey> trigger) where T : class, new()
        {
            if (!_triggers.ContainsKey(typeof (T)))
            {
                lock (((ICollection) _triggers).SyncRoot)
                {
                    if (!_triggers.ContainsKey(typeof (T)))
                    {
                        _triggers.Add(typeof (T), new List<ISterlingTrigger>());
                    }
                }
            }

            _triggers[typeof (T)].Add(trigger);
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
            _byteInterceptorList.Add((T) Activator.CreateInstance(typeof (T)));
        }

        public void UnRegisterInterceptor<T>() where T : BaseSterlingByteInterceptor, new()
        {
            var interceptor = (from i
                                   in _byteInterceptorList
                               where i.GetType().Equals(typeof (T))
                               select i).FirstOrDefault();

            if (interceptor != null)
            {
                _byteInterceptorList.Remove(interceptor);
            }
        }

        /// <summary>
        /// Clears the _byteInterceptorList object
        /// </summary>
        public void UnRegisterInterceptors()
        {
            if (_byteInterceptorList != null)
            {
                _byteInterceptorList.Clear();
            }
        }


        /// <summary>
        ///     Unregister the trigger
        /// </summary>
        /// <param name="trigger">The trigger</param>
        public void UnregisterTrigger<T, TKey>(BaseSterlingTrigger<T, TKey> trigger) where T : class, new()
        {
            if (!_triggers.ContainsKey(typeof (T))) return;

            if (_triggers[typeof (T)].Contains(trigger))
            {
                lock (((ICollection) _triggers).SyncRoot)
                {
                    if (_triggers[typeof (T)].Contains(trigger))
                    {
                        _triggers[typeof (T)].Remove(trigger);
                    }
                }
            }
        }

        /// <summary>
        ///     Fire the triggers for a type
        /// </summary>
        /// <param name="type">The target type</param>
        private IEnumerable<ISterlingTrigger> _TriggerList(Type type)
        {
            if (_triggers.ContainsKey(type))
            {
                List<ISterlingTrigger> triggers;

                lock (((ICollection) _triggers).SyncRoot)
                {
                    triggers = new List<ISterlingTrigger>(_triggers[type]);
                }

                return triggers;
            }

            return Enumerable.Empty<ISterlingTrigger>();
        }

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public virtual string Name { get { return GetType().FullName; } }


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
            TableTypeResolver.RegisterTypeResolver(typeInterceptor);
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
                                                Load<T, TKey>, keyFunction);
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
            lock(((ICollection)TableDefinitions).SyncRoot)
            {
                if (!TableDefinitions.ContainsKey(tableDefinition.TableType))
                {
                    TableDefinitions.Add(tableDefinition.TableType, tableDefinition);
                }
            }
        }

        /// <summary>
        ///     Call to publish tables 
        /// </summary>
        internal void PublishTables(ISterlingDriver driver)
        {
            Driver = driver;

            lock (((ICollection) TableDefinitions).SyncRoot)
            {
                foreach (var table in RegisterTables())
                {
                    if (TableDefinitions.ContainsKey(table.TableType))
                    {
                        throw new SterlingDuplicateTypeException(table.TableType, Name);
                    }
                    TableDefinitions.Add(table.TableType, table);
                }
            }
            Driver.PublishTables(TableDefinitions);
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
        ///     Save it
        /// </summary>
        /// <typeparam name="T">The instance type</typeparam>
        /// <typeparam name="TKey">Save it</typeparam>
        /// <param name="instance">The instance</param>
        public TKey Save<T, TKey>(T instance) where T : class, new()
        {
            return (TKey) Save(typeof (T), instance);
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
        ///     Save an instance against a base class table definition
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <typeparam name="TKey">Save it</typeparam>
        /// <param name="instance">An instance or sub-class of the table type</param>
        /// <returns></returns>
        public TKey SaveAs<T, TKey>(T instance) where T : class, new()
        {
            return (TKey)SaveAs(instance);
        }

        /// <summary>
        ///     Save an instance against a base class table definition
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <param name="instance">An instance or sub-class of the table type</param>
        /// <returns></returns>
        public object SaveAs<T>(T instance) where T : class, new()
        {
            var tableType = typeof(T);
            return Save(instance.GetType(), tableType, instance, new CycleCache());
        }

        /// <summary>
        ///     Save against a base class when key is not known
        /// </summary>
        /// <param name="tableType"></param>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object SaveAs(Type tableType, object instance)
        {
            if (!PlatformAdapter.Instance.IsSubclassOf( instance.GetType(), tableType) || instance.GetType() != tableType)
            {
                throw new SterlingException(string.Format("{0} is not of type {1}", instance.GetType().Name, tableType.Name));
            }

            return Save(tableType, instance);
        }

        /// <summary>
        ///     Entry point for save
        /// </summary>
        /// <param name="type">Type to save</param>
        /// <param name="instance">Instance</param>
        /// <returns>The key saved</returns>
        public object Save(Type type, object instance)
        {
            return Save(type, type, instance, new CycleCache());
        }

        /// <summary>
        ///     Save when key is not known
        /// </summary>
        /// <param name="actualType">The type of instance to save</param>
        /// <param name="tableType">The table type to save to</param>
        /// <param name="instance">The instance</param>
        /// <param name="cache">Cycle cache</param>
        /// <returns>The key</returns>
        public object Save(Type actualType, Type tableType, object instance, CycleCache cache)
        {
            if (!TableDefinitions.ContainsKey(tableType))
            {
                throw new SterlingTableNotFoundException(instance.GetType(), Name);
            }

            if (!TableDefinitions[tableType].IsDirty(instance))
            {
                return TableDefinitions[tableType].FetchKeyFromInstance(instance);
            }

            // call any before save triggers 
            foreach (var trigger in _TriggerList(tableType).Where(trigger => !trigger.BeforeSave(actualType, instance)))
            {
                throw new SterlingTriggerException(
                    Exceptions.Exceptions.BaseDatabaseInstance_Save_Save_suppressed_by_trigger, trigger.GetType());
            }

            var key = TableDefinitions[tableType].FetchKeyFromInstance(instance);            

            int keyIndex;

            if (cache.Check(instance))
            {
                return key;
            }
            
            lock(TableDefinitions[tableType])
            {
                if (cache.Check(instance))
                {
                    return key;
                }

                cache.Add(tableType, instance, key);
            
                keyIndex = TableDefinitions[tableType].Keys.AddKey(key);
            }

            var memStream = new MemoryStream();

            try
            {                
                using (var bw = new BinaryWriter(memStream))
                {
                    Helper.Save(actualType, instance, bw, cache,true);

                    bw.Flush();

                    if (_byteInterceptorList.Count > 0)
                    {
                        var bytes = memStream.ToArray();

                        bytes = _byteInterceptorList.Aggregate(bytes,
                                                               (current, byteInterceptor) =>
                                                               byteInterceptor.Save(current));

                        memStream = new MemoryStream(bytes);
                    }

                    memStream.Seek(0, SeekOrigin.Begin);
                    Driver.Save(tableType, keyIndex, memStream.ToArray());
                }
            }
            finally
            {
                memStream.Flush();
                memStream.Dispose();
            }


            // update the indexes
            foreach (var index in TableDefinitions[tableType].Indexes.Values)
            {
                index.AddIndex(instance, key);
            }

            // call post-save triggers
            foreach (var trigger in _TriggerList(tableType))
            {
                trigger.AfterSave(actualType, instance);
            }

            _RaiseOperation(SterlingOperation.Save, tableType, key);

            return key;
        }

        /// <summary>
        ///     Save when key is not known
        /// </summary>
        /// <typeparam name="T">The type of the instance</typeparam>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object Save<T>(T instance) where T : class, new()
        {
            return Save(typeof (T), instance);
        }

        /// <summary>
        ///     Save asynchronously
        /// </summary>
        /// <typeparam name="T">The type to save</typeparam>
        /// <param name="list">A list of items to save</param>
        /// <returns>A unique identifier for the batch</returns>
        public PendingOperation SaveAsync<T>(IList<T> list)
        {
            return SaveAsync((IList) list);
        }

        /// <summary>
        ///     Non-generic asynchronous save
        /// </summary>
        /// <param name="list">The list of items</param>
        /// <returns>A unique job identifier</returns>
        public PendingOperation SaveAsync(IList list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            if (list.Count == 0)
            {
                return null;
            }

            if (!IsRegistered(list[0].GetType()))
            {
                throw new SterlingTableNotFoundException(list[0].GetType(), Name);
            }

            PendingOperation operation = null;

            Action<CancellationToken, Action<decimal>> work = ( token, progressChanged ) =>
            {
                lock ( ( (ICollection) _workers ).SyncRoot )
                {
                    _workers.Add( operation );
                }

                var index = 0;

                try
                {
                    foreach ( var item in list )
                    {
                        token.ThrowIfCancellationRequested();

                        Save( item.GetType(), item );

                        var pct = index++ * 100 / list.Count;

                        progressChanged( pct );
                    }
                }
                finally
                {
                    lock ( ( (ICollection) _workers ).SyncRoot )
                    {
                        _workers.Remove( operation );
                    }
                }
            };

            return new PendingOperation( work );
        }

        /// <summary>
        ///     Flush all keys and indexes to storage
        /// </summary>
        public void Flush()
        {
            if (_locks == null || !_locks.ContainsKey(GetType())) return;
            
            lock (Lock)
            {
                foreach (var def in TableDefinitions.Values)
                {
                    def.Keys.Flush();
                    
                    foreach (var idx in def.Indexes.Values)
                    {
                        idx.Flush();
                    }
                }                                
            }            

            _RaiseOperation(SterlingOperation.Flush, GetType(), Name);
        }

        /// <summary>
        ///     Load it 
        /// </summary>
        /// <typeparam name="T">The type to load</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <param name="key">The value of the key</param>
        /// <returns>The instance</returns>
        public T Load<T, TKey>(TKey key) where T : class, new()
        {
            return (T) Load(typeof (T), key);
        }

        /// <summary>
        ///     Load it (key type not typed)
        /// </summary>
        /// <typeparam name="T">The type to load</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The instance</returns>
        public T Load<T>(object key) where T : class, new()
        {
            return (T) Load(typeof (T), key);
        }

        /// <summary>
        ///     Load entry point with new cycle cache
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <param name="key">The key</param>
        /// <returns>The object</returns>
        public object Load(Type type, object key)
        {
            return Load(type, key, new CycleCache());
        }

        /// <summary>
        ///     Load it without knowledge of the key type
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <param name="key">The key</param>
        /// <param name="cache">Cache queue</param>
        /// <returns>The instance</returns>
        public object Load(Type type, object key, CycleCache cache)
        {
            var newType = type;
            var assignable = false;
            var keyIndex = -1;

            if (!TableDefinitions.ContainsKey(type))
            {
                // check if type is a base type
                foreach (var t in TableDefinitions.Keys.Where( k => PlatformAdapter.Instance.IsAssignableFrom( type, k ) ) )
                {
                    assignable = true;

                    lock (TableDefinitions[t])
                    {
                        keyIndex = TableDefinitions[t].Keys.GetIndexForKey(key);
                    }

                    if (keyIndex < 0) continue;

                    newType = t;
                    break;
                }
            }
            else
            {
                lock (TableDefinitions[newType])
                {
                    keyIndex = TableDefinitions[newType].Keys.GetIndexForKey(key);
                }
            }

            if (!assignable)
            {
                if (!TableDefinitions.ContainsKey(type))
                {
                    throw new SterlingTableNotFoundException(type, Name);
                }
            }

            if (keyIndex < 0)
            {
                return null;
            }

            var obj = cache.CheckKey(newType, key);

            if (obj != null)
            {
                return obj;
            }

            BinaryReader br = null;
            MemoryStream memStream = null;

            try
            {
                br = Driver.Load(newType, keyIndex);

                var serializationHelper = new SerializationHelper(this, Serializer, SterlingFactory.GetLogger(),
                                                                  s => Driver.GetTypeIndex(s),
                                                                  i => Driver.GetTypeAtIndex(i));
                if (_byteInterceptorList.Count > 0)
                {
                    var bytes = br.ReadBytes((int) br.BaseStream.Length);

                    bytes = _byteInterceptorList.ToArray().Reverse().Aggregate(bytes,
                                                                               (current, byteInterceptor) =>
                                                                               byteInterceptor.Load(current));

                    memStream = new MemoryStream(bytes);

                    br.Dispose();
                    br = new BinaryReader(memStream);
                }
                obj = serializationHelper.Load(newType, key, br, cache);
            }
            finally
            {
                if (br != null)
                {
                    br.Dispose();
                }

                if (memStream != null)
                {
                    memStream.Flush();
                    memStream.Dispose();
                }
            }

            _RaiseOperation(SterlingOperation.Load, newType, key);
            return obj;
        }

        /// <summary>
        ///     Delete it 
        /// </summary>
        /// <typeparam name="T">The type to delete</typeparam>
        /// <param name="instance">The instance</param>
        public void Delete<T>(T instance) where T : class
        {
            Delete(typeof (T), TableDefinitions[typeof (T)].FetchKeyFromInstance(instance));
        }

        /// <summary>
        ///     Delete it (non-generic)
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="key">The key</param>
        public void Delete(Type type, object key)
        {
            if (!TableDefinitions.ContainsKey(type))
            {
                throw new SterlingTableNotFoundException(type, Name);
            }

            // call any before save triggers 
            foreach (var trigger in _TriggerList(type).Where(trigger => !trigger.BeforeDelete(type, key)))
            {
                throw new SterlingTriggerException(
                    string.Format(Exceptions.Exceptions.BaseDatabaseInstance_Delete_Delete_failed_for_type, type),
                    trigger.GetType());
            }

            var keyEntry = TableDefinitions[type].Keys.GetIndexForKey(key);

            Driver.Delete(type, keyEntry);

            TableDefinitions[type].Keys.RemoveKey(key);
            foreach (var index in TableDefinitions[type].Indexes.Values)
            {
                index.RemoveIndex(key);
            }

            _RaiseOperation(SterlingOperation.Delete, type, key);
        }

        /// <summary>
        ///     Truncate all records for a type
        /// </summary>
        /// <param name="type">The type</param>
        public void Truncate(Type type)
        {
            if (_workers.Count > 0)
            {
                throw new SterlingException(
                    Exceptions.Exceptions.BaseDatabaseInstance_Truncate_Cannot_truncate_when_background_operations);
            }

            if (_locks == null || !_locks.ContainsKey(GetType())) return;

            lock (Lock)
            {
                Driver.Truncate(type);

                TableDefinitions[type].Keys.Truncate();
                foreach (var index in TableDefinitions[type].Indexes.Values)
                {
                    index.Truncate();
                }
            }

            _RaiseOperation(SterlingOperation.Truncate, type, null);
        }

        /// <summary>
        ///     Purge the entire database - wipe it clean!
        /// </summary>
        public void Purge()
        {
            if (_locks == null || !_locks.ContainsKey(GetType())) return;

            lock (Lock)
            {
                var tasks = new Task[ _workers.Count ];

                // cancel all async operations, accumulate delays
                lock (((ICollection) _workers).SyncRoot)
                {
                    for( var i = 0; i < _workers.Count; i++ )
                    {
                        var worker = _workers[ i ];

                        tasks[ i ] = worker.Task;
                        worker.Cancel();
                    }
                }

                Task.WaitAll( tasks );

                Driver.Purge();

                // clear key lists from memory
                foreach (var table in TableDefinitions.Keys)
                {
                    TableDefinitions[table].Keys.Truncate();
                    foreach (var index in TableDefinitions[table].Indexes.Values)
                    {
                        index.Truncate();
                    }
                }                
            }

            _RaiseOperation(SterlingOperation.Purge, GetType(), Name);
        }

        /// <summary>
        ///     Refresh indexes and keys from disk
        /// </summary>
        public void Refresh()
        {
            foreach (var table in TableDefinitions)
            {
                table.Value.Refresh();
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