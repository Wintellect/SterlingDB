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
    public interface ISterlingDatabase 
    {
        SterlingEngine Engine { get; }

        LogManager LogManager { get; }
        
        /// <summary>
        ///     Backup the database
        /// </summary>
        /// <typeparam name="T">The database type</typeparam>
        /// <param name="writer">Writer to receive the backup</param>
        Task BackupAsync<T>(BinaryWriter writer) where T : BaseDatabaseInstance;

        /// <summary>
        ///     Restore the database
        /// </summary>
        /// <typeparam name="T">The database type</typeparam>
        /// <param name="reader">The stream providing the backup</param>
        Task RestoreAsync<T>(BinaryReader reader) where T : BaseDatabaseInstance;

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