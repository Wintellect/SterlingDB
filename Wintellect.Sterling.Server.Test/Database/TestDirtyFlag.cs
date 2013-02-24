
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

using System;
using System.Collections.Generic;
using System.Linq;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
    public class DirtyDatabase : BaseDatabaseInstance
    {
        public Predicate<TestModel> Predicate { get; set; }

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
                               .WithDirtyFlag<TestModel,int>(o=>this.Predicate(o))
                           };
        }
    }

#if SILVERLIGHT
    [Tag("Dirty")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestDirtyFlagAltDriver : TestDirtyFlag
    {
        protected override ISterlingDriver GetDriver( string test )
        {
#if NETFX_CORE
            return new WindowsStorageDriver( test );
#elif SILVERLIGHT
            return new IsolatedStorageDriver( test );
#else
            return new FileSystemDriver( test );
#endif
        }
    }

#if SILVERLIGHT
    [Tag("Dirty")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestDirtyFlag : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        public TestDirtyFlag()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<DirtyDatabase>( GetDriver( TestContext.TestName ) );
            ( (DirtyDatabase) _databaseInstance ).Predicate = model => true;
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

            ( (DirtyDatabase) _databaseInstance ).Predicate = model => true;

            // now check that all were accessed
            _databaseInstance.SaveAsync( actual ).Wait();

            var accessed = (from t in actual.Children where !t.Accessed select 1).Any();

            Assert.IsFalse(accessed, "Dirty flag on save failed: some children were not accessed.");
        }

        [TestMethod]
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

            ( (DirtyDatabase) _databaseInstance ).Predicate = model => false;

            // now check that none were accessed
            _databaseInstance.SaveAsync( actual ).Wait();

            var accessed = (from t in actual.Children where t.Accessed select 1).Any();

            Assert.IsFalse(accessed, "Dirty flag on save failed: some children were accessed.");
        }

    }
}