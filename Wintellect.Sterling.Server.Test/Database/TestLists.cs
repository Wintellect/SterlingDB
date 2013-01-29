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
#if SILVERLIGHT 
    [Tag("List")]
#endif
    [TestClass]
    public class TestLists
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
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod][Timeout(1000)]
        public void TestNullList()
        {
            var expected = TestListModel.MakeTestListModel();
            expected.Children = null;
            var key = _databaseInstance.SaveAsync( expected ).Result;
            var actual = _databaseInstance.LoadAsync<TestListModel>(key).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNull(actual.Children, "Save/load failed: list should be null.");            
        }

        [TestMethod][Timeout(1000)]
        public void TestEmptyList()
        {
            var expected = TestListModel.MakeTestListModel();
            expected.Children.Clear();
            var key = _databaseInstance.SaveAsync(expected).Result;
            var actual = _databaseInstance.LoadAsync<TestListModel>(key).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNotNull(actual.Children, "Save/load failed: list not initialized.");
            Assert.AreEqual(0, actual.Children.Count, "Save/load failed: list size mismatch.");            
        }

        [TestMethod][Timeout(1000)]
        public void TestList()
        {
            var expected = TestListModel.MakeTestListModel();
            var key = _databaseInstance.SaveAsync(expected).Result;
            var actual = _databaseInstance.LoadAsync<TestListModel>(key).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNotNull(actual.Children, "Save/load failed: list not initialized.");
            Assert.AreEqual(expected.Children.Count, actual.Children.Count, "Save/load failed: list size mismatch.");
            for (var x = 0; x < expected.Children.Count; x++)
            {
                Assert.AreEqual(expected.Children[x].Key, actual.Children[x].Key, "Save/load failed: key mismatch.");
                Assert.AreEqual(expected.Children[x].Data, actual.Children[x].Data, "Save/load failed: data mismatch.");                
            }
        }

        [TestMethod][Timeout(1000)]
        public void TestModelAsList()
        {
            var expected = TestModelAsListModel.MakeTestModelAsList();
            var key = _databaseInstance.SaveAsync(expected).Result;
            var actual = _databaseInstance.LoadAsync<TestModelAsListModel>( key ).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.Id, actual.Id, "Save/load failed: key mismatch.");
            Assert.AreEqual(expected.Count, actual.Count, "Save/load failed: list size mismatch.");
            for (var x = 0; x < expected.Count; x++)
            {
                Assert.AreEqual(expected[x].Key, actual[x].Key, "Save/load failed: key mismatch.");
                Assert.AreEqual(expected[x].Data, actual[x].Data, "Save/load failed: data mismatch.");
            }
        }

        [TestMethod][Timeout(1000)]
        public void TestModelAsListWithParentReference()
        {
            var expected = TestModelAsListModel.MakeTestModelAsListWithParentReference();
            var key = _databaseInstance.SaveAsync( expected ).Result;

            var actual = _databaseInstance.LoadAsync<TestModelAsListModel>( key ).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.Id, actual.Id, "Save/load failed: key mismatch.");
            Assert.AreEqual(expected.Count, actual.Count, "Save/load failed: list size mismatch.");
            for (var x = 0; x < expected.Count; x++)
            {
                Assert.AreEqual(expected[x].Key, actual[x].Key, "Save/load failed: key mismatch.");
                Assert.AreEqual(expected[x].Data, actual[x].Data, "Save/load failed: data mismatch.");
                Assert.AreEqual(expected, expected[x].Parent, "Parent doesn't match");
            }
        }

    }
}