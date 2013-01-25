using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Events;
using Wintellect.Sterling.Core.Indexes;
using Wintellect.Sterling.Core.Keys;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestDatabaseInterfaceInstance : ISterlingDatabaseInstance
    {
        private static readonly object _lock = new object();
        public object Lock
        {
            get { return _lock; }
        }

        /// <summary>
        ///     The driver
        /// </summary>
        public ISterlingDriver Driver
        {
            get { throw new NotImplementedException(); }
        }

        public SerializationHelper Helper
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        ///     Register a trigger
        /// </summary>
        /// <param name="trigger">The trigger</param>
        public void RegisterTrigger<T, TKey>(BaseSterlingTrigger<T, TKey> trigger) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Unregister the trigger
        /// </summary>
        /// <param name="trigger">The trigger</param>
        public void UnregisterTrigger<T, TKey>(BaseSterlingTrigger<T, TKey> trigger) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register byte stream interceptors
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RegisterInterceptor<T>() where T : BaseSterlingByteInterceptor, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register byte stream interceptors
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UnRegisterInterceptor<T>() where T : BaseSterlingByteInterceptor, new()
        {
            throw new NotImplementedException();
        }

        public void UnRegisterInterceptors()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public string Name
        {
            get { return "Test Database Instance"; }
        }

        /// <summary>
        ///     The type dictating which objects should be ignored
        /// </summary>
        public Type IgnoreAttribute { get { return typeof(SterlingIgnoreAttribute); } }

        /// <summary>
        ///     True if it is registered with the sterling engine
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>True if it can be persisted</returns>
        public bool IsRegistered<T>(T instance) where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Non-generic registration check
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>True if it is registered</returns>
        public bool IsRegistered(Type type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Get the key for an object
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object GetKey(object instance)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Get the key for an object
        /// </summary>
        /// <param name="table">The instance type</param>
        /// <returns>The key type</returns>
        public Type GetKeyType(Type table)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save it
        /// </summary>
        /// <typeparam name="T">The instance type</typeparam>
        /// <typeparam name="TKey">Save it</typeparam>
        /// <param name="instance">The instance</param>
        public TKey Save<T, TKey>(T instance) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Query (keys only)
        /// </summary>
        /// <typeparam name="T">The type to query</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <returns>The list of keys to query</returns>
        public List<TableKey<T, TKey>> Query<T, TKey>() where T: class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Query (index)
        /// </summary>
        /// <typeparam name="T">The type to query</typeparam>
        /// <typeparam name="TIndex">The type of the index</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="indexName">The name of the index</param>
        /// <returns>The list of indexes to query</returns>
        public List<TableIndex<T, TIndex, TKey>> Query<T, TIndex, TKey>(string indexName) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Query (index)
        /// </summary>
        /// <typeparam name="T">The type to query</typeparam>
        /// <typeparam name="TIndex1">The type of the index</typeparam>
        /// <typeparam name="TIndex2">The type of the index</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="indexName">The name of the index</param>
        /// <returns>The list of indexes to query</returns>  
        public List<TableIndex<T, Tuple<TIndex1, TIndex2>, TKey>> Query<T, TIndex1, TIndex2, TKey>(string indexName) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save a sub-class under a base class table definition
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <typeparam name="TKey">Save it</typeparam>
        /// <param name="instance">An instance or sub-class of the table type</param>
        /// <returns></returns>
        public TKey SaveAs<T, TKey>(T instance) where T : class,new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save a sub-class under a base class table definition
        /// </summary>
        /// <typeparam name="T">The table type</typeparam>
        /// <param name="instance">The instance or sub-class of the table type</param>
        /// <returns></returns>
        public object SaveAs<T>(T instance) where T : class,new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save it (no knowledge of key)
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object Save<T>(T instance) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save when key is not known
        /// </summary>
        /// <param name="actualType">The type of instance to save</param>
        /// <param name="tableType">The type used to find the table to save to</param>
        /// <param name="instance">The instance</param>
        /// <param name="cache">The cycle cache</param>
        /// <returns>The key</returns>
        public object Save(Type actualType, Type tableType, object instance, CycleCache cache)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save when key is not known
        /// </summary>
        /// <param name="type">The type to save</param>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object Save(Type type, object instance)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save against a base class when key is not known
        /// </summary>
        /// <param name="type">The table type to save against</param>
        /// <param name="instance">The instance</param>
        /// <returns>The key</returns>
        public object SaveAs(Type type, object instance)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Save asynchronously
        /// </summary>
        /// <typeparam name="T">The type to save</typeparam>
        /// <param name="list">A list of items to save</param>
        /// <returns>A unique identifier for the batch</returns>
        public PendingOperation SaveAsync<T>(IList<T> list)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Non-generic asynchronous save
        /// </summary>
        /// <param name="list">The list of items</param>
        /// <returns>A unique job identifier</returns>
        public PendingOperation SaveAsync(IList list)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Flush all keys and indexes to storage
        /// </summary>
        public void Flush()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Load it (key type not typed)
        /// </summary>
        /// <typeparam name="T">The type to load</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The instance</returns>
        public T Load<T>(object key) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Load it without knowledge of the key type
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <param name="key">The key</param>
        /// <param name="cache">The cycle cache</param>
        /// <returns>The instance</returns>
        public object Load(Type type, object key, CycleCache cache)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Load it without knowledge of the key type
        /// </summary>
        /// <param name="type">The type to load</param>
        /// <param name="key">The key</param>
        /// <returns>The instance</returns>
        public object Load(Type type, object key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Delete it 
        /// </summary>
        /// <typeparam name="T">The type to delete</typeparam>
        /// <param name="instance">The instance</param>
        public void Delete<T>(T instance) where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Delete it (non-generic)
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="key">The key</param>
        public void Delete(Type type, object key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Truncate all records for a type
        /// </summary>
        /// <param name="type">The type</param>
        public void Truncate(Type type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Purge the entire database - wipe it clean!
        /// </summary>
        public void Purge()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Refresh indexes and keys from disk
        /// </summary>
        public void Refresh()
        {
            throw new NotImplementedException();
        }

#pragma warning disable 0067
        public event EventHandler<SterlingOperationArgs> SterlingOperationPerformed;

        /// <summary>
        ///     Create a table definition
        /// </summary>
        /// <typeparam name="T">The type of the table</typeparam>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="keyFunction">Function to return the key</param>
        /// <returns>The table definition</returns>
        public ITableDefinition CreateTableDefinition<T, TKey>(Func<T, TKey> keyFunction) where T : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Get the list of table definitions
        /// </summary>
        /// <returns>The list of table definitions</returns>
        public IEnumerable<ITableDefinition> GetTableDefinitions()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Register a new table definition
        /// </summary>
        /// <param name="tableDefinition">The new table definition</param>
        public void RegisterTableDefinition(ITableDefinition tableDefinition)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Registers a property converter.
        /// </summary>
        /// <param name="propertyConverter">The property converter</param>
        public void RegisterPropertyConverter(ISterlingPropertyConverter propertyConverter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the property converter for the given type, or returns null if none is found.
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="propertyConverter">The property converter</param>
        /// <returns>True if there is a registered property converter.</returns>
        public bool TryGetPropertyConverter(Type type, out ISterlingPropertyConverter propertyConverter)
        {
            throw new NotImplementedException();
        }
    }
}
