namespace Wintellect.Sterling.Core.Keys
{
    /// <summary>
    ///     Key collection interface
    /// </summary>
    public interface IKeyCollection
    {
        /// <summary>
        ///     Serialize
        /// </summary>
        void Flush();

        /// <summary>
        ///     Get the index for a key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The index</returns>
        int GetIndexForKey(object key);

        /// <summary>
        ///     Refresh the list
        /// </summary>
        void Refresh();

        /// <summary>
        ///     Truncate the collection
        /// </summary>
        void Truncate();

        /// <summary>
        ///     Add a key to the list
        /// </summary>
        /// <param name="key">The key</param>
        int AddKey(object key);

        /// <summary>
        ///     Remove a key from the list
        /// </summary>
        /// <param name="key">The key</param>
        void RemoveKey(object key);
    }
}