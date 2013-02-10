using System.Collections.Generic;
#if SILVERLIGHT
using Microsoft.Phone.Testing;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Database
{
    public class NullableClass
    {
        public int Id { get; set; }
        public int? Value { get; set; }
    }

    public class NullableDatabase : BaseDatabaseInstance
    {
        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public override string Name
        {
            get { return "Nullable"; }
        }

        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                           {
                               CreateTableDefinition<NullableClass, int>(n => n.Id)
                           };
        }
    }

#if SILVERLIGHT
    [Tag("Nullable")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestNullable
    {                
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        [TestInitialize]
        public void TestInit()
        {            
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<NullableDatabase>();
            _databaseInstance.PurgeAsync().Wait();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestNotNull()
        {
            var test = new NullableClass {Id = 1, Value = 1};
            _databaseInstance.SaveAsync( test ).Wait();
            var actual = _databaseInstance.LoadAsync<NullableClass>( 1 ).Result;
            Assert.AreEqual(test.Id, actual.Id, "Failed to load nullable with nullable set: key mismatch.");
            Assert.AreEqual(test.Value, actual.Value, "Failed to load nullable with nullable set: value mismatch.");
        }

        [TestMethod]
        public void TestNull()
        {
            var test = new NullableClass { Id = 1, Value = null };
            _databaseInstance.SaveAsync( test ).Wait();
            var actual = _databaseInstance.LoadAsync<NullableClass>( 1 ).Result;
            Assert.AreEqual(test.Id, actual.Id, "Failed to load nullable with nullable set: key mismatch.");
            Assert.IsNull(actual.Value, "Failed to load nullable with nullable set: value mismatch.");
        }
    }
}