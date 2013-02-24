
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

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("AggregateList")]
#endif
    [TestClass]
    public class TestAggregateListAltDriver : TestAggregateList
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
    [Tag("AggregateList")]
#endif
    [TestClass]
    public class TestAggregateList : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>( GetDriver( TestContext.TestName ) );
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestNullList()
        {
            var expected = TestAggregateListModel.MakeTestAggregateListModel();
            expected.Children = null;
            var key = _databaseInstance.SaveAsync( expected ).Result;
            var actual = _databaseInstance.LoadAsync<TestAggregateListModel>( key ).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNull(actual.Children, "Save/load failed: list should be null.");            
        }

        [TestMethod]
        public void TestEmptyList()
        {
            var expected = TestAggregateListModel.MakeTestAggregateListModel();
            expected.Children.Clear();
            var key = _databaseInstance.SaveAsync( expected ).Result;
            var actual = _databaseInstance.LoadAsync<TestAggregateListModel>( key ).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNotNull(actual.Children, "Save/load failed: list not initialized.");
            Assert.AreEqual(0, actual.Children.Count, "Save/load failed: list size mismatch.");
        }

        [TestMethod]
        public void TestList()
        {
            var expected = TestAggregateListModel.MakeTestAggregateListModel();
            _databaseInstance.SaveAsync(expected).Wait();
            var actual = _databaseInstance.LoadAsync<TestAggregateListModel>( expected.ID ).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNotNull(actual.Children, "Save/load failed: list not initialized.");
            Assert.AreEqual(expected.Children.Count, actual.Children.Count, "Save/load failed: list size mismatch.");

            for (var x = 0; x < expected.Children.Count; x++)
            {
                Assert.AreEqual(expected.Children[x].Key, actual.Children[x].Key, "Save/load failed: key mismatch.");
                Assert.AreEqual(expected.Children[x].BaseProperty, actual.Children[x].BaseProperty, "Save/load failed: data mismatch.");
                Assert.AreEqual(expected.Children[x].GetType(), actual.Children[x].GetType(), "Save/load failed: type mismatch.");
            }
        }
    }
}
