using System;
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
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
    [TestClass]
    public class TestDelete
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.Purge();
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

            var key = _databaseInstance.Save(testModel);

            Assert.AreEqual(1, countKeys(), "Keys not updated with save.");
            Assert.AreEqual(1, countIndex(), "Index count not updated with save.");

            var actual = _databaseInstance.Load<TestModel>(key);

            Assert.IsNotNull(actual, "Test model did not re-load.");

            _databaseInstance.Delete(actual);

            Assert.AreEqual(0, countKeys(), "Database updated with invalid key count after delete.");
            Assert.AreEqual(0, countIndex(), "Database updated with invalid index count after delete.");

            actual = _databaseInstance.Load<TestModel>(key);

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

            var key = _databaseInstance.Save(testModel);

            Assert.AreEqual(1, countKeys(), "Keys not updated with save.");
            Assert.AreEqual(1, countIndex(), "Index count not updated with save.");

            var actual = _databaseInstance.Load<TestModel>(key);

            Assert.IsNotNull(actual, "Test model did not re-load.");

            _databaseInstance.Delete(typeof(TestModel), key);

            Assert.AreEqual(0, countKeys(), "Database updated with invalid key count after delete.");
            Assert.AreEqual(0, countIndex(), "Database updated with invalid index count after delete.");

            actual = _databaseInstance.Load<TestModel>(key);

            Assert.IsNull(actual, "Delete failed: loaded actual value after delete.");
        }
    }
}
