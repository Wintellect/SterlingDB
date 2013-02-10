using System.Threading.Tasks;
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
        Task FlushAsync();
        
        /// <summary>
        ///     Refresh the list
        /// </summary>
        Task RefreshAsync();

        /// <summary>
        ///     Truncate index
        /// </summary>
        Task TruncateAsync();

        /// <summary>
        ///     Add an index to the list
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The related key</param>
        Task AddIndexAsync(object instance, object key);

        /// <summary>
        ///     Update the index
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <param name="key">The key</param>
        Task UpdateIndexAsync(object instance, object key);

        /// <summary>
        ///     Remove an index from the list
        /// </summary>
        /// <param name="key">The key</param>
        Task RemoveIndexAsync(object key);
    }
}