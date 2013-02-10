using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    ///     The sterling database manager
    /// </summary>
    internal class SterlingDatabase : ISterlingDatabase
    {        
        private static bool _activated;

        /// <summary>
        ///     Master list of databases
        /// </summary>
        private readonly Dictionary<string,Tuple<Type,ISterlingDatabaseInstance>> _databases = new Dictionary<string,Tuple<Type,ISterlingDatabaseInstance>>();
        
        /// <summary>
        ///     The main serializer
        /// </summary>
        private ISterlingSerializer _serializer = new AggregateSerializer();

        /// <summary>
        ///     Logger
        /// </summary>
        private readonly LogManager _logManager;

        internal SterlingDatabase(LogManager logger)
        {
            _logManager = logger;
        }
        
        /// <summary>
        ///     Registers a logger (multiple loggers may be registered)
        /// </summary>
        /// <param name="log">The call for logging</param>
        /// <returns>A unique identifier for the logger</returns>
        public Guid RegisterLogger(Action<SterlingLogLevel, string, Exception> log)
        {
            return _logManager.RegisterLogger(log);
        }

        /// <summary>
        ///     Unhooks a logging mechanism
        /// </summary>
        /// <param name="guid">The guid</param>
        public void UnhookLogger(Guid guid)
        {
            _logManager.UnhookLogger(guid);
        }

        /// <summary>
        ///     Log a message 
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="message">The message data</param>
        /// <param name="exception">The exception</param>
        public void Log(SterlingLogLevel level, string message, Exception exception)
        {
            _logManager.Log(level, message, exception);
        }

        private static readonly Guid _databaseVersion = new Guid("da202921-e258-4de5-b9d7-5b71ac83ff72");

        /// <summary>
        ///     Back up the database
        /// </summary>
        /// <typeparam name="T">The database type</typeparam>
        /// <param name="writer">A writer to receive the backup</param>
        public Task BackupAsync<T>(BinaryWriter writer) where T : BaseDatabaseInstance
        {
            return Task.Factory.StartNew( () =>
                {
                    _RequiresActivation();

                    var databaseQuery = from d in _databases where d.Value.Item1.Equals( typeof( T ) ) select d.Value.Item2;
                    if ( !databaseQuery.Any() )
                    {
                        throw new SterlingDatabaseNotFoundException( typeof( T ).FullName );
                    }
                    var database = databaseQuery.First();

                    database.FlushAsync().Wait();

                    // first write the version
                    _serializer.Serialize( _databaseVersion, writer );

                    var typeMaster = database.Driver.GetTypesAsync().Result;

                    // now the type master
                    writer.Write( typeMaster.Count );
                    foreach ( var type in typeMaster )
                    {
                        writer.Write( type );
                    }

                    // now iterate tables
                    foreach ( var table in ( (BaseDatabaseInstance) database ).TableDefinitions )
                    {
                        // get the key list
                        var keys = database.Driver.DeserializeKeysAsync( table.Key, table.Value.KeyType,
                                                                   table.Value.GetNewDictionary() ).Result;

                        // reality check
                        if ( keys == null )
                        {
                            writer.Write( 0 );
                        }
                        else
                        {
                            // write the count for the keys
                            writer.Write( keys.Count );

                            // for each key, serialize it out along with the object - indexes  can be rebuilt on the flipside
                            foreach ( var key in keys.Keys )
                            {
                                _serializer.Serialize( key, writer );
                                writer.Write( (int) keys[ key ] );

                                // get the instance 
                                using ( var instance = database.Driver.LoadAsync( table.Key, (int) keys[ key ] ).Result )
                                {
                                    var bytes = instance.ReadBytes( (int) instance.BaseStream.Length );
                                    writer.Write( bytes.Length );
                                    writer.Write( bytes );
                                }
                            }
                        }
                    }
                } );
        }

        /// <summary>
        ///     Restore the database
        /// </summary>
        /// <typeparam name="T">Type of the database</typeparam>
        /// <param name="reader">The reader with the backup information</param>
        public Task RestoreAsync<T>(BinaryReader reader) where T : BaseDatabaseInstance
        {
            return Task.Factory.StartNew( () =>
                {
                    _RequiresActivation();

                    var databaseQuery = from d in _databases where d.Value.Item1.Equals( typeof( T ) ) select d.Value.Item2;
                    if ( !databaseQuery.Any() )
                    {
                        throw new SterlingDatabaseNotFoundException( typeof( T ).FullName );
                    }
                    var database = databaseQuery.First();

                    database.PurgeAsync().Wait();

                    // read the version
                    var version = _serializer.Deserialize<Guid>( reader );

                    if ( !version.Equals( _databaseVersion ) )
                    {
                        throw new SterlingException( string.Format( "Unexpected database version." ) );
                    }

                    var typeMaster = new List<string>();

                    var count = reader.ReadInt32();

                    for ( var x = 0; x < count; x++ )
                    {
                        typeMaster.Add( reader.ReadString() );
                    }

                    database.Driver.DeserializeTypesAsync( typeMaster ).Wait();

                    foreach ( var table in ( (BaseDatabaseInstance) database ).TableDefinitions )
                    {
                        // make the dictionary 
                        var keyDictionary = table.Value.GetNewDictionary();

                        if ( keyDictionary == null )
                        {
                            throw new SterlingException( string.Format( "Unable to make dictionary for key type {0}",
                                                                      table.Value.KeyType ) );
                        }

                        var keyCount = reader.ReadInt32();
                        for ( var record = 0; record < keyCount; record++ )
                        {
                            var key = _serializer.Deserialize( table.Value.KeyType, reader );
                            var keyIndex = reader.ReadInt32();
                            keyDictionary.Add( key, keyIndex );

                            var size = reader.ReadInt32();
                            var bytes = reader.ReadBytes( size );
                            database.Driver.SaveAsync( table.Key, keyIndex, bytes ).Wait();
                        }

                        database.Driver.SerializeKeysAsync( table.Key, table.Value.KeyType, keyDictionary ).Wait();

                        // now refresh the table
                        table.Value.RefreshAsync().Wait();

                        // now generate the indexes 
                        if ( table.Value.Indexes.Count <= 0 ) continue;

                        var table1 = table;

                        foreach ( var key in keyDictionary.Keys )
                        {
                            var instance = database.LoadAsync( table1.Key, key ).Result;

                            foreach ( var index in table.Value.Indexes )
                            {
                                index.Value.AddIndexAsync( instance, key ).Wait();
                            }
                        }

                        foreach ( var index in table.Value.Indexes )
                        {
                            index.Value.FlushAsync().Wait();
                        }

                    }
                } );
        }

        public ISterlingDatabaseInstance RegisterDatabase<T>() where T : BaseDatabaseInstance
        {
            return RegisterDatabase<T>(null);
        }

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        /// <typeparam name="TDriver">Register with a driver</typeparam>
        public ISterlingDatabaseInstance RegisterDatabase<T, TDriver>() where T : BaseDatabaseInstance where TDriver : ISterlingDriver
        {
            var driver = (TDriver) Activator.CreateInstance(typeof (TDriver));
            return RegisterDatabase<T>(driver);
        }

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        /// <typeparam name="TDriver">Register with a driver</typeparam>
        public ISterlingDatabaseInstance RegisterDatabase<T, TDriver>(TDriver driver)
            where T : BaseDatabaseInstance
            where TDriver : ISterlingDriver
        {
            return RegisterDatabase<T>(driver);
        }

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        public ISterlingDatabaseInstance RegisterDatabase<T>(ISterlingDriver driver) where T : BaseDatabaseInstance
        {
            _RequiresActivation();
            _logManager.Log(SterlingLogLevel.Information, 
                string.Format("Sterling is registering database {0}", typeof(T)),
                null);  
            
            if ((from d in _databases where d.Value.Item1.Equals(typeof(T)) select d).Count() > 0)
            {
                throw new SterlingDuplicateDatabaseException(typeof(T));
            }
            
             var database = (ISterlingDatabaseInstance)Activator.CreateInstance(typeof (T));

            if (driver == null)
            {
                driver = new MemoryDriver(database.Name, _serializer, _logManager.Log);
            }
            else
            {
                driver.DatabaseName = database.Name;
                driver.DatabaseSerializer = _serializer;
                driver.Log = _logManager.Log;
            }
            
            ((BaseDatabaseInstance) database).Serializer = _serializer;

            ((BaseDatabaseInstance)database).RegisterTypeResolvers();
            ((BaseDatabaseInstance)database).RegisterPropertyConverters();
            
            ((BaseDatabaseInstance)database).PublishTables(driver);
            _databases.Add(database.Name, new Tuple<Type, ISterlingDatabaseInstance>(typeof(T),database));
            return database;
        }

        /// <summary>
        ///     Unloads/flushes the database instances
        /// </summary>
        private void _Unload()
        {
            foreach (var database in
                _databases.Values.Select(databaseDef => databaseDef.Item2).OfType<BaseDatabaseInstance>())
            {
                database.Unload();
            }
        }

        /// <summary>
        ///     Retrieve the database with the name
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <returns>The database instance</returns>
        public ISterlingDatabaseInstance GetDatabase(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("databaseName");
            }
            
            _RequiresActivation();
            
            if (!_databases.ContainsKey(databaseName))
            {
                throw new SterlingDatabaseNotFoundException(databaseName);
            }
            
            return _databases[databaseName].Item2;
        }

        /// <summary>
        ///     Register a serializer with the system
        /// </summary>
        /// <typeparam name="T">The type of the serliaizer</typeparam>
        public void RegisterSerializer<T>() where T : BaseSerializer
        {
            if (_activated)
            {
                throw new SterlingActivationException(string.Format("RegisterSerializer<{0}>", typeof(T).FullName));
            }

            ((AggregateSerializer)_serializer).AddSerializer((ISterlingSerializer)Activator.CreateInstance(typeof(T)));
        }

        /// <summary>
        /// Register a class responsible for type resolution.
        /// </summary>
        /// <param name="typeResolver">The typeResolver</param>
        public void RegisterTypeResolver(ISterlingTypeResolver typeResolver)
        {
            TableTypeResolver.RegisterTypeResolver(typeResolver);
        }

        /// <summary>
        ///     Must be called to activate the engine. 
        ///     Can only be called once.
        /// </summary>
        public void Activate()
        {
            lock(Lock)
            {
                if (_activated)
                {
                    throw new SterlingActivationException("Activate()");
                }
                _LoadDefaultSerializers();
                _activated = true;                
            }
        }

        private void _LoadDefaultSerializers()
        {
            // Load default serializes
            RegisterSerializer<DefaultSerializer>();  
            RegisterSerializer<ExtendedSerializer>();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Deactivate()
        {
            lock(Lock)
            {
                _activated = false;                
                _Unload();
                _databases.Clear();
                BaseDatabaseInstance.Deactivate();
                _serializer = new AggregateSerializer();
            }

            return;
        }

        /// <summary>
        ///     Requires that sterling is activated
        /// </summary>
        private static void _RequiresActivation()
        {
            if (!_activated)
            {
                throw new SterlingNotReadyException();
            }
        }

        private static readonly object _lock = new object();

        public object Lock
        {
            get { return _lock; }
        }
    }
}
