
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
using System.Linq;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
    [TestClass]
    public class TestDeleteListAltDriver : TestDelete
    {
        protected override ISterlingDriver GetDriver()
        {
#if NETFX_CORE
            return new WindowsStorageDriver();
#elif SILVERLIGHT
            return new IsolatedStorageDriver();
#else
            return new FileSystemDriver();
#endif
        }
    }

    [TestClass]
    public class TestDelete : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>( GetDriver() );
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestDatabaseDeleteByInstance()
        {
            var testModel = TestModel.MakeTestModel();
            
            Func<int> countKeys = () => _databaseInstance.Query<TestModel, int>().Count();
            Func<int> countIndex =
                () => _databaseInstance.Query<TestModel, string, int>(TestDatabaseInstance.DATAINDEX).Count();
            
            Assert.AreEqual(0, countKeys(),"Database initialized with invalid key count.");
            Assert.AreEqual(0, countIndex(), "Database initialized with invalid index count.");

            var key = _databaseInstance.SaveAsync(testModel).Result;

            Assert.AreEqual(1, countKeys(), "Keys not updated with save.");
            Assert.AreEqual(1, countIndex(), "Index count not updated with save.");

            var actual = _databaseInstance.LoadAsync<TestModel>( key ).Result;

            Assert.IsNotNull(actual, "Test model did not re-load.");

            _databaseInstance.DeleteAsync( actual ).Wait();

            Assert.AreEqual(0, countKeys(), "Database updated with invalid key count after delete.");
            Assert.AreEqual(0, countIndex(), "Database updated with invalid index count after delete.");

            actual = _databaseInstance.LoadAsync<TestModel>( key ).Result;

            Assert.IsNull(actual, "Delete failed: loaded actual value after delete.");
        }

        [TestMethod]
        public void TestDatabaseDeleteByKey()
        {
            var testModel = TestModel.MakeTestModel();

            Func<int> countKeys = () => _databaseInstance.Query<TestModel, int>().Count();
            Func<int> countIndex =
                () => _databaseInstance.Query<TestModel, string, int>(TestDatabaseInstance.DATAINDEX).Count();

            Assert.AreEqual(0, countKeys(), "Database initialized with invalid key count.");
            Assert.AreEqual(0, countIndex(), "Database initialized with invalid index count.");

            var key = _databaseInstance.SaveAsync( testModel ).Result;

            Assert.AreEqual(1, countKeys(), "Keys not updated with save.");
            Assert.AreEqual(1, countIndex(), "Index count not updated with save.");

            var actual = _databaseInstance.LoadAsync<TestModel>( key ).Result;

            Assert.IsNotNull(actual, "Test model did not re-load.");

            _databaseInstance.DeleteAsync( typeof( TestModel ), key ).Wait();

            Assert.AreEqual(0, countKeys(), "Database updated with invalid key count after delete.");
            Assert.AreEqual(0, countIndex(), "Database updated with invalid index count after delete.");

            actual = _databaseInstance.LoadAsync<TestModel>( key ).Result;

            Assert.IsNull(actual, "Delete failed: loaded actual value after delete.");
        }
    }
}
