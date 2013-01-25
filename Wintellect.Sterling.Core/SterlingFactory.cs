using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    ///     Factory to retrieve the sterling manager
    /// </summary>
    internal static class SterlingFactory
    {
        /// <summary>
        ///     Instance of the database
        /// </summary>
        private static ISterlingDatabase _database; 

        /// <summary>
        ///     The log manager
        /// </summary>
        private static LogManager _logManager;     

        static SterlingFactory()
        {
            Initialize();
        }

        internal static void Initialize()
        {
            _logManager = new LogManager();          
            _database = new SterlingDatabase(_logManager);
        }

        /// <summary>
        ///     Gets the database engine
        /// </summary>
        /// <returns>The instance of the database engine</returns>
        public static ISterlingDatabase GetDatabaseEngine()
        {
            return _database;
        }

        /// <summary>
        ///     Logger
        /// </summary>
        /// <returns>The logger</returns>
        internal static LogManager GetLogger()
        {
            return _logManager;
        }        
    }
}
