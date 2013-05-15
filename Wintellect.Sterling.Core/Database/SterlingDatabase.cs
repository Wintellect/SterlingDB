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
        private bool _activated;
        private readonly object _lock = new object();

        /// <summary>
        ///     Master list of databases
        /// </summary>
        private readonly Dictionary<string,Tuple<Type,ISterlingDatabaseInstance>> _databases = new Dictionary<string,Tuple<Type,ISterlingDatabaseInstance>>();

        /// <summary>
        ///     The main serializer
        /// </summary>
        private ISterlingSerializer _serializer = null;

        /// <summary>
        ///     Logger
        /// </summary>
        private readonly LogManager _logManager = new LogManager();

        public SterlingDatabase( SterlingEngine engine )
        {
            this.Engine = engine;
            this.TypeResolver = new TableTypeResolver();
            _serializer = new AggregateSerializer( engine.PlatformAdapter );
        }

        public SterlingEngine Engine { get; private set; }

        public LogManager LogManager
        {
            get { return _logManager; }
        }

        private static readonly Guid _databaseVersion = new Guid("da202921-e258-4de5-b9d7-5b71ac83ff72");

        /// <summary>
        ///     Back up the database
        /// </summary>
        /// <typeparam name="T">The database type</typeparam>
        /// <param name="writer">A writer to receive the backup</param>
        public async Task BackupAsync<T>(BinaryWriter writer) where T : BaseDatabaseInstance
        {
            _RequiresActivation();

            var databaseQuery = from d in _databases where d.Value.Item1.Equals( typeof( T ) ) select d.Value.Item2;
            
            if ( !databaseQuery.Any() )
            {
                throw new SterlingDatabaseNotFoundException( typeof( T ).FullName );
            }
            
            var database = databaseQuery.First();

            await database.FlushAsync().ConfigureAwait( false );

            // first write the version
            _serializer.Serialize( _databaseVersion, writer );

            var typeMaster = await database.Driver.GetTypesAsync().ConfigureAwait( false );

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
                var keys = await database.Driver.DeserializeKeysAsync( table.Key, table.Value.KeyType, table.Value.GetNewDictionary() ).ConfigureAwait( false );

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
                        using ( var instance = await database.Driver.LoadAsync( table.Key, (int) keys[ key ] ).ConfigureAwait( false ) )
                        {
                            var bytes = instance.ReadBytes( (int) instance.BaseStream.Length );
                            writer.Write( bytes.Length );
                            writer.Write( bytes );
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Restore the database
        /// </summary>
        /// <typeparam name="T">Type of the database</typeparam>
        /// <param name="reader">The reader with the backup information</param>
        public async Task RestoreAsync<T>(BinaryReader reader) where T : BaseDatabaseInstance
        {
            _RequiresActivation();

            var databaseQuery = from d in _databases where d.Value.Item1.Equals( typeof( T ) ) select d.Value.Item2;

            if ( !databaseQuery.Any() )
            {
                throw new SterlingDatabaseNotFoundException( typeof( T ).FullName );
            }
            
            var database = databaseQuery.First();

            await database.PurgeAsync().ConfigureAwait( false );

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

            await database.Driver.DeserializeTypesAsync( typeMaster ).ConfigureAwait( false );

            foreach ( var table in ( (BaseDatabaseInstance) database ).TableDefinitions )
            {
                // make the dictionary 
                var keyDictionary = table.Value.GetNewDictionary();

                if ( keyDictionary == null )
                {
                    throw new SterlingException( string.Format( "Unable to make dictionary for key type {0}", table.Value.KeyType ) );
                }

                var keyCount = reader.ReadInt32();

                for ( var record = 0; record < keyCount; record++ )
                {
                    var key = _serializer.Deserialize( table.Value.KeyType, reader );
                    var keyIndex = reader.ReadInt32();
                    keyDictionary.Add( key, keyIndex );

                    var size = reader.ReadInt32();
                    var bytes = reader.ReadBytes( size );

                    await database.Driver.SaveAsync( table.Key, keyIndex, bytes ).ConfigureAwait( false );
                }

                await database.Driver.SerializeKeysAsync( table.Key, table.Value.KeyType, keyDictionary ).ConfigureAwait( false );

                // now refresh the table
                await table.Value.RefreshAsync().ConfigureAwait( false );

                // now generate the indexes 
                if ( table.Value.Indexes.Count <= 0 ) continue;

                var table1 = table;

                foreach ( var key in keyDictionary.Keys )
                {
                    var instance = await database.LoadAsync( table1.Key, key ).ConfigureAwait( false );

                    foreach ( var index in table.Value.Indexes )
                    {
                        await index.Value.AddIndexAsync( instance, key ).ConfigureAwait( false );
                    }
                }

                foreach ( var index in table.Value.Indexes )
                {
                    await index.Value.FlushAsync().ConfigureAwait( false );
                }
            }
        }

        public ISterlingDatabaseInstance RegisterDatabase<T>( string instanceName = null ) where T : BaseDatabaseInstance
        {
            return RegisterDatabase<T>( instanceName ?? "InMemory", null );
        }

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        /// <typeparam name="TDriver">Register with a driver</typeparam>
        public ISterlingDatabaseInstance RegisterDatabase<T, TDriver>( string instanceName ) where T : BaseDatabaseInstance where TDriver : ISterlingDriver
        {
            var driver = (TDriver) Activator.CreateInstance(typeof (TDriver));
            return RegisterDatabase<T>( instanceName, driver);
        }

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        /// <typeparam name="TDriver">Register with a driver</typeparam>
        public ISterlingDatabaseInstance RegisterDatabase<T, TDriver>(string instanceName, TDriver driver)
            where T : BaseDatabaseInstance
            where TDriver : ISterlingDriver
        {
            return RegisterDatabase<T>(instanceName, driver);
        }

        /// <summary>
        ///     Register a database type with the system
        /// </summary>
        /// <typeparam name="T">The type of the database to register</typeparam>
        public ISterlingDatabaseInstance RegisterDatabase<T>(string instanceName, ISterlingDriver driver) where T : BaseDatabaseInstance
        {
            _RequiresActivation();
            _logManager.Log(SterlingLogLevel.Information, 
                string.Format("Sterling is registering database {0}", typeof(T)),
                null);

            var existing = _databases.Values.FirstOrDefault( tuple => tuple.Item1 == typeof ( T ) );

            if ( existing != null )
            {
                return existing.Item2;
            }

            var instance = (BaseDatabaseInstance)Activator.CreateInstance(typeof (T));

            instance.Database = this;

            if (driver == null)
            {
                driver = new MemoryDriver();
            }

            driver.DatabaseInstanceName = instanceName;
            driver.DatabaseSerializer = _serializer;
            driver.Log = _logManager.Log;

            instance.Serializer = _serializer;

            instance.RegisterTypeResolvers();
            instance.RegisterPropertyConverters();
            instance.PublishTables(driver);
            
            _databases.Add(instanceName, new Tuple<Type, ISterlingDatabaseInstance>(typeof(T),instance));
            
            return instance;
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

            ISterlingSerializer serializer = null;

            if ( typeof ( T ) == typeof ( AggregateSerializer ) )
            {
                serializer = new AggregateSerializer( this.Engine.PlatformAdapter );
            }
            else if ( typeof ( T ) == typeof ( ExtendedSerializer ) )
            {
                serializer = new ExtendedSerializer( this.Engine.PlatformAdapter );
            }
            else
            {
                serializer = (ISterlingSerializer) Activator.CreateInstance( typeof ( T ) );
            }

            ( (AggregateSerializer) _serializer ).AddSerializer( serializer );
        }

        internal TableTypeResolver TypeResolver { get; set; }
        
        /// <summary>
        /// Register a class responsible for type resolution.
        /// </summary>
        /// <param name="typeResolver">The typeResolver</param>
        public void RegisterTypeResolver(ISterlingTypeResolver typeResolver)
        {
            TypeResolver.RegisterTypeResolver(typeResolver);
        }

        /// <summary>
        ///     Must be called to activate the engine. 
        ///     Can only be called once.
        /// </summary>
        public void Activate()
        {
            lock ( _lock )
            {
                if ( ! _activated )
                {
                    _LoadDefaultSerializers();
                    _activated = true;
                }
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
            lock ( _lock )
            {
                if ( _activated )
                {
                    _activated = false;
                    _Unload();
                    _databases.Clear();
                    _serializer = new AggregateSerializer( this.Engine.PlatformAdapter );
                }
            }
        }

        /// <summary>
        ///     Requires that sterling is activated
        /// </summary>
        private void _RequiresActivation()
        {
            if (!_activated)
            {
                throw new SterlingNotReadyException();
            }
        }
    }
}
