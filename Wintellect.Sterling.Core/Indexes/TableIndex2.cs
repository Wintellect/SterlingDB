using System;

namespace Wintellect.Sterling.Core.Indexes
{
    /// <summary>
    ///     An individual table key
    /// </summary>
    /// <typeparam name="T">The class the key maps to</typeparam>
    /// <typeparam name="TIndex1">The type of the index</typeparam>
    /// <typeparam name="TIndex2">The type of the second index</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    public class TableIndex<T, TIndex1, TIndex2, TKey> : TableIndex<T,Tuple<TIndex1,TIndex2>,TKey> where T : class, new()
    {        
        /// <summary>
        ///     Index
        /// </summary>
        public TIndex1 Index1 { get; private set; }

        /// <summary>
        ///     Second index
        /// </summary>
        public TIndex2 Index2 { get; private set;}

        //// <param name="getter">The getter</param>
        /// <summary>
        ///     Construct with how to get the key
        /// </summary>
        /// <param name="index2">Value of the second index</param>
        /// <param name="key">The associated key with the index</param>
        /// <param name="getter">Getter method for loading an instance</param>
        /// <param name="index1">Value of the first index</param>
        internal TableIndex(TIndex1 index1, TIndex2 index2, TKey key, Func<TKey, T> getter) : base(Tuple.Create(index1,index2), key, getter)
        {
            Index1 = index1;
            Index2 = index2;
        }

        internal TableIndex(TableIndex<T, Tuple<TIndex1, TIndex2>, TKey> baseIndex, Func<TKey, T> getter) : base(baseIndex.Index, baseIndex.Key, getter)
        {            
        }

        /// <summary>
        ///     To string
        /// </summary>
        /// <returns>The key</returns>
        public override string ToString()
        {
            return string.Format("Index: [{0}][{1},{2}]={3},{4}", typeof(T).FullName, typeof(TIndex1).FullName, typeof(TIndex2).FullName, Index1, Index2);
        }
    }
}
