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
    public class CycleClass
    {
        public int Id { get; set; }
        public int Value { get; set; }
        public CycleClass ChildCycle { get; set; }
    }

    public class CycleDatabase : BaseDatabaseInstance
    {
        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public override string Name
        {
            get { return "Cycle"; }
        }

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
    public class TestCycle
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<CycleDatabase>();
            _databaseInstance.Purge();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.Purge();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestCycleNegativeCase()
        {
            var test = new CycleClass { Id = 1, Value = 1 };
            var child = new CycleClass {Id = 2, Value = 5 };            
            test.ChildCycle = child;
            
            _databaseInstance.Save(test);
            var actual = _databaseInstance.Load<CycleClass>(1);
            Assert.AreEqual(test.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch.");
            Assert.AreEqual(test.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch.");
            Assert.IsNotNull(test.ChildCycle, "Failed to load cycle with non-null child: child is null.");
            Assert.AreEqual(child.Id, actual.ChildCycle.Id, "Failed to load cycle with non-null child: child key mismatch.");
            Assert.AreEqual(child.Value, actual.ChildCycle.Value, "Failed to load cycle with non-null child: value mismatch.");
            
            actual = _databaseInstance.Load<CycleClass>(2);
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

            _databaseInstance.Save(test);
            var actual = _databaseInstance.Load<CycleClass>(1);
            Assert.AreEqual(test.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch.");
            Assert.AreEqual(test.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch.");
            Assert.IsNotNull(test.ChildCycle, "Failed to load cycle with non-null child: child is null.");
            Assert.AreEqual(child.Id, actual.ChildCycle.Id, "Failed to load cycle with non-null child: child key mismatch.");
            Assert.AreEqual(child.Value, actual.ChildCycle.Value, "Failed to load cycle with non-null child: value mismatch.");

            actual = _databaseInstance.Load<CycleClass>(2);
            Assert.AreEqual(child.Id, actual.Id, "Failed to load cycle with non-null child: key mismatch on direct child load.");
            Assert.AreEqual(child.Value, actual.Value, "Failed to load cycle with non-null child: value mismatch on direct child load.");
        }        

    }
}