using System.Threading.Tasks;
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
        Task FlushAsync();

        /// <summary>
        ///     Get the index for a key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The index</returns>
        Task<int> GetIndexForKeyAsync(object key);

        /// <summary>
        ///     Refresh the list
        /// </summary>
        Task RefreshAsync();

        /// <summary>
        ///     Truncate the collection
        /// </summary>
        Task TruncateAsync();

        /// <summary>
        ///     Add a key to the list
        /// </summary>
        /// <param name="key">The key</param>
        Task<int> AddKeyAsync(object key);

        /// <summary>
        ///     Remove a key from the list
        /// </summary>
        /// <param name="key">The key</param>
        Task RemoveKeyAsync(object key);
    }
}