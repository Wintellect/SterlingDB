using System;
using System.Collections;
using System.Collections.Generic;

namespace Wintellect.Sterling.Core.Keys
{
    /// <summary>
    ///     Collection of keys for a given entity
    /// </summary>
    internal class KeyCollection<T,TKey> : IKeyCollection where T: class, new()
    {
        private readonly Func<TKey, T> _resolver;
        private readonly ISterlingDriver _driver;
        
        /// <summary>
        ///     Set when keys change
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        ///     Initialize the key collection
        /// </summary>
        /// <param name="driver">Driver</param>
        /// <param name="resolver">The resolver for loading the object</param>
        public KeyCollection(ISterlingDriver driver, Func<TKey,T> resolver)
        {
            _driver = driver;
            _resolver = resolver;                       
            _DeserializeKeys();
            IsDirty = false;        
        }        

        /// <summary>
        ///     The list of keys
        /// </summary>
        private readonly List<TableKey<T,TKey>> _keyList = new List<TableKey<T, TKey>>();

        /// <summary>
        ///     Map for keys in the set
        /// </summary>
        private readonly Dictionary<TKey,int> _keyMap = new Dictionary<TKey, int>();

        /// <summary>
        ///     Force to a new list so the internal one cannot be manipulated directly
        /// </summary>
        public List<TableKey<T, TKey>> Query { get { return new List<TableKey<T, TKey>>(_keyList); } }

        private void _DeserializeKeys()
        {
            _keyList.Clear();
            _keyMap.Clear();

            var keyMap = _driver.DeserializeKeys(typeof (T), typeof(TKey), new Dictionary<TKey, int>()) ?? new Dictionary<TKey, int>();

            if (keyMap.Count > 0)
            {
                foreach (var key in keyMap.Keys)
                {
                    var idx = (int)keyMap[key];
                    if (idx >= NextKey)
                    {
                        NextKey = idx + 1;
                    }
                    _keyMap.Add((TKey)key, idx);
                    _keyList.Add(new TableKey<T, TKey>((TKey)key, _resolver));
                }
            }
            else
            {
                NextKey = 0;
            }            
        }

        /// <summary>
        ///     Serializes the key list
        /// </summary>
        private void _SerializeKeys()
        {
            _driver.SerializeKeys(typeof(T), typeof(TKey), _keyMap);            
        }

        /// <summary>
        ///     The next key
        /// </summary>
        internal int NextKey { get; private set; } 

        /// <summary>
        ///     Serialize
        /// </summary>
        public void Flush()
        {
            lock (((ICollection)_keyList).SyncRoot)
            {
                if (IsDirty)
                {
                    _SerializeKeys();
                }
                IsDirty = false;             
            }
        }

        /// <summary>
        ///     Get the index for a key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The index</returns>
        public int GetIndexForKey(object key)
        {
            return _keyMap.ContainsKey((TKey) key) ? _keyMap[(TKey) key] : -1; 
        }

        /// <summary>
        ///     Refresh the list
        /// </summary>
        public void Refresh()
        {
            lock (((ICollection)_keyList).SyncRoot)
            {
                if (IsDirty)
                {
                    _SerializeKeys();
                }
                _DeserializeKeys();
                IsDirty = false;               
            }
        }

        /// <summary>
        ///     Truncate the collection
        /// </summary>
        public void Truncate()
        {
            IsDirty = false;
            Refresh();
        }       

        /// <summary>
        ///     Add a key to the list
        /// </summary>
        /// <param name="key">The key</param>
        public int AddKey(object key)
        {
            lock (((ICollection)_keyList).SyncRoot)
            {
                var newKey = new TableKey<T, TKey>((TKey) key, _resolver);

                if (!_keyList.Contains(newKey))
                {
                    _keyList.Add(newKey);
                    _keyMap.Add((TKey) key, NextKey++);
                    IsDirty = true;                   
                }
                else
                {
                    var idx = _keyList.IndexOf(newKey);
                    _keyList[idx].Refresh();
                }
            }

            return _keyMap[(TKey)key];
        }

        /// <summary>
        ///     Remove a key from the list
        /// </summary>
        /// <param name="key">The key</param>
        public void RemoveKey(object key)
        {
            lock (((ICollection)_keyList).SyncRoot)
            {
                var checkKey = new TableKey<T, TKey>((TKey) key, _resolver);

                if (!_keyList.Contains(checkKey)) return;
                _keyList.Remove(checkKey);
                _keyMap.Remove((TKey) key);
                IsDirty = true;             
            }
        }
    }
}
