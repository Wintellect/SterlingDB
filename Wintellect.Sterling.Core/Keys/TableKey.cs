using System;

namespace Wintellect.Sterling.Core.Keys
{
    /// <summary>
    ///     An individual table key
    /// </summary>
    /// <typeparam name="T">The class the key maps to</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    public class TableKey<T,TKey> where T: class, new()
    {
        private readonly Func<TKey, T> _getter;
        private readonly int _hashCode;

        /// <summary>
        ///     Construct with how to get the key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="getter">The getter</param>
        public TableKey(TKey key, Func<TKey,T> getter)
        {
            Key = key;
            _hashCode = key.GetHashCode();
            _getter = getter;
            LazyValue = new Lazy<T>( () => _getter(Key) );
        }

        /// <summary>
        ///     Key
        /// </summary>
        public TKey Key { get; private set; }

        /// <summary>
        ///     Entity the key points to
        /// </summary>
        public Lazy<T> LazyValue { get; private set; }

        /// <summary>
        ///     Refresh the lazy value
        /// </summary>
        public void Refresh()
        {
            LazyValue = new Lazy<T>(() => _getter(Key));
        }

        /// <summary>
        ///     Compares for equality
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns>True if equal</returns>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == _hashCode && ((TableKey<T, TKey>) obj).Key.Equals(Key);
        }

        /// <summary>
        ///     Hash code
        /// </summary>
        /// <returns>The has code of the key</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>The key</returns>
        public override string ToString()
        {
            return string.Format("Key: [{0}][{1}]={2}", typeof (T).FullName, typeof (TKey).FullName, Key);
        }
    }
}
