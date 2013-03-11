
#if NETFX_CORE
using Wintellect.Sterling.WinRT.WindowsStorage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#elif SILVERLIGHT
using Microsoft.Phone.Testing;
using Wintellect.Sterling.WP8.IsolatedStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Wintellect.Sterling.Server.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System.Collections.Generic;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Database
{
    public class CycleClass
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public CycleClass ChildCycle { get; set; }
    }

    public class CycleDatabase : BaseDatabaseInstance
    {
        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                           {
                               CreateTableDefinition<CycleClass, int>(n => n.Id)
                           };
        }
    }

#if SILVERLIGHT
    [Tag("Cycle")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestCycleAltDriver : TestCycle
    {
        protected override ISterlingDriver GetDriver()
        {
#if NETFX_CORE
            return new WindowsStorageDriver();
#elif SILVERLIGHT
            return new IsolatedStorageDriver();
#elif AZURE_DRIVER
            return new Wintellect.Sterling.Server.Azure.TableStorage.Driver();
#else
            return new FileSystemDriver();
#endif
        }
    }

#if SILVERLIGHT
    [Tag("Cycle")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestCycle : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<CycleDatabase>( TestContext.TestName, GetDriver() );
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
        public void TestCycleNegativeCase()
        {
            var test = new CycleClass { Id = 1, Value = 1 };
            var child = new CycleClass {Id = 2, Value = 5 };            
            test.ChildCycle = child;

            _databaseInstance.SaveAsync( test ).Wait();
            var actual = _databaseInstance.LoadAsync<CycleClass>( 1 ).Result;
            Assert.AreEqual(test.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch.");
            Assert.AreEqual(test.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch.");
            Assert.IsNotNull(test.ChildCycle, "Failed to load cycle with non-null child: child is null.");
            Assert.AreEqual(child.Id, actual.ChildCycle.Id, "Failed to load cycle with non-null child: child key mismatch.");
            Assert.AreEqual(child.Value, actual.ChildCycle.Value, "Failed to load cycle with non-null child: value mismatch.");

            actual = _databaseInstance.LoadAsync<CycleClass>( 2 ).Result;
            Assert.AreEqual(child.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch on direct child load.");
            Assert.AreEqual(child.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch on direct child load.");            
        }

        [TestMethod] 
        public void TestCyclePositiveCase()
        {
            var test = new CycleClass { Id = 1, Value = 1 };
            var child = new CycleClass { Id = 2, Value = 5 };
            test.ChildCycle = child;
            child.ChildCycle = test; // this creates our cycle condition

            _databaseInstance.SaveAsync( test ).Wait();
            var actual = _databaseInstance.LoadAsync<CycleClass>( 1 ).Result;
            Assert.AreEqual(test.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch.");
            Assert.AreEqual(test.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch.");
            Assert.IsNotNull(test.ChildCycle, "Failed to load cycle with non-null child: child is null.");
            Assert.AreEqual(child.Id, actual.ChildCycle.Id, "Failed to load cycle with non-null child: child key mismatch.");
            Assert.AreEqual(child.Value, actual.ChildCycle.Value, "Failed to load cycle with non-null child: value mismatch.");

            actual = _databaseInstance.LoadAsync<CycleClass>( 2 ).Result;
            Assert.AreEqual(child.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch on direct child load.");
            Assert.AreEqual(child.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch on direct child load.");
        }        

    }
}