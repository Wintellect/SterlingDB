using System;
using System.Collections.Generic;
using System.Linq;
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
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
    public class DirtyDatabase : BaseDatabaseInstance
    {       

        /// <summary>
        ///     The name of the database instance
        /// </summary>
        public override string Name
        {
            get { return "DirtyDatabase"; }
        }

        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                           {
                               CreateTableDefinition<TestListModel, int>(t=>t.ID),
                               CreateTableDefinition<TestModel, int>(t=>t.Key)
                               .WithDirtyFlag<TestModel,int>(o=>TestDirtyFlag.IsTestDirty(o))
                           };
        }
    }

#if SILVERLIGHT
    [Tag("Dirty")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestDirtyFlag
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public static Predicate<TestModel> IsTestDirty = model => true;

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<DirtyDatabase>();
            _databaseInstance.PurgeAsync().Wait();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod][Timeout(1000)]
        public void TestDirtyFlagFalse()
        {
            var expected = TestListModel.MakeTestListModel();

            // first save is to generate the keys
            var key = _databaseInstance.SaveAsync( expected ).Result;

            var actual = _databaseInstance.LoadAsync<TestListModel>( key ).Result;

            foreach(var model in actual.Children)
            {
                model.ResetAccess();
            }

            // set so it is always dirty
            IsTestDirty = model => true;

            // now check that all were accessed
            _databaseInstance.SaveAsync( actual ).Wait();

            var accessed = (from t in actual.Children where !t.Accessed select 1).Any();

            Assert.IsFalse(accessed, "Dirty flag on save failed: some children were not accessed.");
        }

        [TestMethod][Timeout(1000)]
        public void TestDirtyFlagTrue()
        {
            var expected = TestListModel.MakeTestListModel();

            // first save is to generate the keys
            var key = _databaseInstance.SaveAsync( expected ).Result;

            var actual = _databaseInstance.LoadAsync<TestListModel>( key ).Result;

            foreach (var model in actual.Children)
            {
                model.ResetAccess();
            }

            // set so it is never dirty
            IsTestDirty = model => false;

            // now check that none were accessed
            _databaseInstance.SaveAsync( actual ).Wait();

            var accessed = (from t in actual.Children where t.Accessed select 1).Any();

            Assert.IsFalse(accessed, "Dirty flag on save failed: some children were accessed.");
        }

    }
}