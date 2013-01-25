using System;
using System.Collections.Generic;
using System.Linq;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    ///     Cycle cache for cycle detection
    /// </summary>
    public class CycleCache : List<CycleItem>
    {
        /// <summary>
        ///     Add an item to the cache
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="instance">The instance</param>
        /// <param name="key">The key</param>
        public void Add(Type type, object instance, object key)
        {
            if (instance == null || key == null)
            {
                return; 
            }

            Add(new CycleItem {ClassType = type, Instance = instance, Key = key});
        }

        /// <summary>
        ///     Check for existance based on key and return if there
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="key">The key</param>
        /// <returns>The cached instance, if it exists</returns>
        public object CheckKey(Type type, object key)
        {
            if (key == null)
            {
                return null;
            }

            return (from o in this
                    where o.ClassType.Equals(type) && key.Equals(o.Key)
                    select o.Instance).FirstOrDefault();
        }

        /// <summary>
        ///     Check to see if an instance already exists
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>True if it does</returns>
        public bool Check(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            return (from o in this
                    where ReferenceEquals(instance, o.Instance)
                    select o).Any();
        }
    }
}