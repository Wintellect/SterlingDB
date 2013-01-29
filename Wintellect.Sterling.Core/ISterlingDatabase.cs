using System;
using System.IO;
using System.Threading.Tasks;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    ///     Sterling database interface
    /// </summary>
    public interface ISterlingDatabase : ISterlingLock 
    {
        /// <summary>
        ///     Registers a logger (multiple loggers may be registered)
        /// </summary>
        /// <param name="log">The call for logging</param>
        /// <returns>A unique identifier for the logger</returns>
        Guid RegisterLogger(Action<SterlingLogLevel, string, Exception> log);        

        /// <summary>
        ///     Unhooks a logging mechanism
        /// </summary>
        /// <param name="guid">The guid</param>
        void UnhookLogger(Guid guid);
        
        /// <summary>
        ///     Log a message 
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="message">The message data</param>
        /// <param name="exception">The exception</param>
        void Log(SterlingLogLevel level, string message, Exception exception);

        /// <summary>
        ///     Backup the database
        /// </summary>
        /// <typeparam name="T">The database type</typeparam>
        /// <param name="writer">Writer to receive the backup</param>
        void Backup<T>(BinaryWriter writer) where T : BaseDatabaseInstance;

        /// <summary>
        ///     Restore the database
        /// </summary>
        /// <typeparam name="T">The database type</typeparam>
        /// <param name="reader">The stream providing the backup</param>
        void Restore<T>(BinaryReader reader) where T : BaseDatabaseInstance;

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        ISterlingDatabaseInstance RegisterDatabase<T>() where T : BaseDatabaseInstance;

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        /// <typeparam name="TDriver">Register with a driver</typeparam>
        ISterlingDatabaseInstance RegisterDatabase<T, TDriver>()
            where T : BaseDatabaseInstance
            where TDriver : ISterlingDriver;

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        ISterlingDatabaseInstance RegisterDatabase<T>(ISterlingDriver driver)
            where T : BaseDatabaseInstance;           


        /// <summary>
        ///     Retrieve the database with the name
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <returns>The database instance</returns>
        ISterlingDatabaseInstance GetDatabase(string databaseName);

        /// <summary>
        ///     Register a serializer with the system
        /// </summary>
        /// <typeparam name="T">The type of the serliaizer</typeparam>
        void RegisterSerializer<T>() where T : BaseSerializer;

        /// <summary>
        /// Register a class responsible for type resolution.
        /// </summary>
        /// <param name="typeResolver">The typeResolver</param>
        void RegisterTypeResolver(ISterlingTypeResolver typeResolver);
    }
}