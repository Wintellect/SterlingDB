namespace Wintellect.Sterling.Core.Indexes
{
    /// <summary>
    ///     Index collection interface
    /// </summary>
    public interface IIndexCollection
    {
        /// <summary>
        ///     Serialize
        /// </summary>
        void Flush();
        
        /// <summary>
        ///     Refresh the list
        /// </summary>
        void Refresh();

        /// <summary>
        ///     Truncate index
        /// </summary>
        void Truncate();

        /// <summary>
        ///     Add an index to the list
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The related key</param>
        void AddIndex(object instance, object key);

        /// <summary>
        ///     Update the index
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The key</param>
        void UpdateIndex(object instance, object key);

        /// <summary>
        ///     Remove an index from the list
        /// </summary>
        /// <param name="key">The key</param>
        void RemoveIndex(object key);
    }
}