using System;
using System.Collections.Generic;
using System.IO;
using Wintellect.Sterling.Core;

namespace Wintellect.Sterling.WinRT.WindowsStorage
{
    /// <summary>
    ///     Path provider
    /// </summary>
    internal class PathProvider
    {  
        private const string TABLEMASTER = "TableMaster";
            
        public const string TYPE = "types.dat";
        public const string KEY = "keys.dat";
        
        /// <summary>
        ///     Master index of tables 
        /// </summary>
        private readonly Dictionary<int, Dictionary<Type, int>> _tableMaster =
            new Dictionary<int, Dictionary<Type, int>>();

        /// <summary>
        ///     Validate base path
        /// </summary>
        /// <param name="basePath"></param>
        private static void _ContractForBasePath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentNullException("basePath");
            }

            if (!basePath.EndsWith(@"/"))
            {
                throw new ArgumentOutOfRangeException("basePath");
            }
        }

        /// <summary>
        ///     Validate database
        /// </summary>
        /// <param name="databaseName">The database name</param>
        private static void _ContractForDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("databaseName");
            }
        }

        /// <summary>
        ///     Contract for driver
        /// </summary>
        /// <param name="driver">The driver</param>
        private static void _ContractForDriver(ISterlingDriver driver)
        {
            if (driver == null)
            {
                throw new ArgumentNullException("driver");
            }
        }

        /// <summary>
        ///     Contract for table type
        /// </summary>
        /// <param name="tableType">The table type</param>
        private static void _ContractForTableType(Type tableType)
        {
            if (tableType == null)
            {
                throw new ArgumentException("tableType");
            }
        }

        /// <summary>
        ///     Contract for table type
        /// </summary>
        /// <param name="indexName">The index name</param>
        private static void _ContractForIndexName(string indexName)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentException("indexName");
            }
        }
        
        /// <summary>
        ///     Get the path for a database
        /// </summary>
        /// <param name="basePath">The base path</param>
        /// <param name="databaseName">The database name</param>
        /// <param name="driver"The driver></param>
        /// <returns>The path</returns>
        public string GetDatabasePath(string basePath, string databaseName, ISterlingDriver driver)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForDriver(driver);
            
            driver.Log(SterlingLogLevel.Verbose,
                            string.Format("Path Provider: Database Path Request: {0}", databaseName), null);

            var path = Path.Combine(basePath, databaseName) + "/";

            driver.Log(SterlingLogLevel.Verbose, string.Format("Resolved database path from {0} to {1}",
                                                                    databaseName, path), null);
            return path;
        }

        /// <summary>
        ///     Generic table path
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="databaseName">The name of the database</param>
        /// <param name="tableType"></param>
        /// <param name="driver"></param>
        /// <returns>The table path</returns>
        public string GetTablePath(string basePath, string databaseName, Type tableType, ISterlingDriver driver)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForTableType(tableType);
            _ContractForDriver(driver);

            driver.Log(SterlingLogLevel.Verbose,
                            string.Format("Path Provider: Table Path Request: {0}", tableType.FullName), null);

            var path = Path.Combine(GetDatabasePath(basePath, databaseName, driver),
                                tableType.FullName) + "/";

            driver.Log(SterlingLogLevel.Verbose, string.Format("Resolved table path from {0} to {1}",
                                                                    tableType.FullName, path), null);
            return path;
        }

        public string GetIndexPath(string basePath, string databaseName, Type tableType, ISterlingDriver driver, string indexName)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForTableType(tableType);
            _ContractForDriver(driver);
            _ContractForIndexName(indexName);
            return Path.Combine(GetTablePath(basePath, databaseName, tableType, driver), string.Format("{0}.idx", indexName));
        }

        /// <summary>
        ///     Gets the folder to a specific instance
        /// </summary>
        /// <remarks>
        ///     Iso slows when there are many files in a given folder, so this allows
        ///     for partitioning of folders
        /// </remarks>
        /// <param name="basePath">Base path</param>
        /// <param name="databaseName">The database</param>
        /// <param name="tableType">The type of the table</param>
        /// <param name="driver">The driver</param>
        /// <param name="keyIndex">The key index</param>
        /// <returns>The path</returns>
        public string GetInstanceFolder(string basePath, string databaseName, Type tableType, ISterlingDriver driver, int keyIndex)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForTableType(tableType);
            _ContractForDriver(driver);
            return Path.Combine(GetTablePath(basePath, databaseName, tableType, driver), (keyIndex/100).ToString()) + "/";                
        }

        /// <summary>
        ///     Gets the path to a specific instance
        /// </summary>
        /// <param name="basePath">Base path</param>
        /// <param name="databaseName">The database</param>
        /// <param name="tableType">The type of the table</param>
        /// <param name="driver">The driver</param>
        /// <param name="keyIndex">The key index</param>
        /// <returns>The path</returns>
        public string GetInstancePath(string basePath, string databaseName, Type tableType, ISterlingDriver driver, int keyIndex)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForTableType(tableType);
            _ContractForDriver(driver);
            return Path.Combine(GetInstanceFolder(basePath, databaseName, tableType, driver, keyIndex), keyIndex.ToString());
        }

        /// <summary>
        ///     Get keys path
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="databaseName"></param>
        /// <param name="tableType"></param>
        /// <param name="driver"></param>
        /// <returns></returns>
        public string GetKeysPath(string basePath, string databaseName, Type tableType, ISterlingDriver driver)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForTableType(tableType);
            _ContractForDriver(driver);
            return Path.Combine(GetTablePath(basePath, databaseName, tableType, driver), KEY);
        }

        /// <summary>
        ///     Get types path
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="databaseName"></param>
        /// <param name="driver"></param>
        /// <returns></returns>
        public string GetTypesPath(string basePath, string databaseName, ISterlingDriver driver)
        {
            _ContractForBasePath(basePath);
            _ContractForDatabaseName(databaseName);
            _ContractForDriver(driver);
            return Path.Combine(GetDatabasePath(basePath, databaseName, driver), TYPE);
        }  
    }
}