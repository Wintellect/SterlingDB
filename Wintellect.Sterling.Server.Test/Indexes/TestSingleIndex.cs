
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
using System.Linq;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Indexes;
using Wintellect.Sterling.Core.Serialization;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Indexes
{
    [Ignore]
    [TestClass]
    public class TestSingleIndexAltDriver : TestSingleIndex
    {
        protected override ISterlingDriver GetDriver( string test )
        {
#if NETFX_CORE
            return new WindowsStorageDriver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#elif SILVERLIGHT
            return new IsolatedStorageDriver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#elif AZURE_DRIVER
            return new Wintellect.Sterling.Server.Azure.TableStorage.Driver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#else
            return new FileSystemDriver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#endif
        }
    }

    [Ignore]
    [TestClass]
    public class TestSingleIndex : TestBase
    {
        private IndexCollection<TestModel, string, int> _target;
        private List<TestModel> _testModels;
        protected readonly ISterlingDatabaseInstance _testDatabase = new TestDatabaseInterfaceInstance();
        private int _testAccessCount;
        private ISterlingDriver _driver;

        /// <summary>
        ///     Fetcher - also flag the fetch
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The model</returns>
        private TestModel _GetTestModelByKey(int key)
        {
            _testAccessCount++;
            return (from t in _testModels where t.Key.Equals(key) select t).FirstOrDefault();
        }

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Init()
        {
            _driver = GetDriver( TestContext.TestName );

            _testModels = new List<TestModel>(3);
            for(var x = 0; x < 3; x++)
            {
                _testModels.Add(TestModel.MakeTestModel());
            }
            
            _testAccessCount = 0;                        
            _target = new IndexCollection<TestModel, string, int>("TestIndex",_driver,
                                                      tm => tm.Data , _GetTestModelByKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _driver.PurgeAsync().Wait();
            _driver = null;
        }

        [TestMethod]
        public void TestAddIndex()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            _target.AddIndexAsync(_testModels[0], _testModels[0].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");            
        }

        [TestMethod]
        public void TestAddDuplicateIndex()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            _target.AddIndexAsync(_testModels[0], _testModels[0].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(_target.Query.Count(),1, "Index count is incorrect.");
            _target.AddIndexAsync(_testModels[0], _testModels[0].Key).Wait();
            Assert.AreEqual(_target.Query.Count(), 1, "Index count is incorrect.");            
        }

        [TestMethod]
        public void TestRemoveIndex()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            _target.AddIndexAsync(_testModels[0], _testModels[0].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(1, _target.Query.Count(), "Index count is incorrect.");
            _target.RemoveIndexAsync(_testModels[0].Key).Wait();
            Assert.AreEqual(0, _target.Query.Count(), "Index was not removed.");
        }

        [TestMethod]
        public void TestQueryable()
        {
            _target.AddIndexAsync(_testModels[0],_testModels[0].Key).Wait();
            _target.AddIndexAsync(_testModels[1], _testModels[1].Key).Wait();
            _target.AddIndexAsync(_testModels[2], _testModels[2].Key).Wait();
            Assert.AreEqual(3, _target.Query.Count(), "Key count is incorrect.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testIndex = (from k in _target.Query where k.Index.Equals(_testModels[1].Data) select k).FirstOrDefault();
            Assert.IsNotNull(testIndex, "Test key not retrieved.");
            Assert.AreEqual(_testModels[1].Key, testIndex.Key, "Key mismatch.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testModel = testIndex.Value.Result;
            Assert.AreSame(_testModels[1], testModel, "Model does not match.");
            Assert.AreEqual(1, _testAccessCount, "Lazy loader access count is incorrect.");
        }

        [TestMethod]
        public void TestSerialization()
        {
            _target.AddIndexAsync(_testModels[0], _testModels[0].Key).Wait();
            _target.AddIndexAsync(_testModels[1], _testModels[1].Key).Wait();            
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set.");
            _target.FlushAsync().Wait();
            Assert.IsFalse(_target.IsDirty, "Dirty flag not reset on flush.");

            var secondTarget = new IndexCollection<TestModel, string, int>("TestIndex", _driver,
                                                                 tm => tm.Data,
                                                                 _GetTestModelByKey);

            // are we able to grab things?
            Assert.AreEqual(2, secondTarget.Query.Count(), "Key count is incorrect.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testIndex = (from k in secondTarget.Query where k.Index.Equals(_testModels[1].Data) select k).FirstOrDefault();
            Assert.IsNotNull(testIndex, "Test index not retrieved.");
            Assert.AreEqual(_testModels[1].Key, testIndex.Key, "Key mismatch.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testModel = testIndex.Value.Result;
            Assert.AreSame(_testModels[1], testModel, "Model does not match.");
            Assert.AreEqual(1, _testAccessCount, "Lazy loader access count is incorrect.");

            // now let's test refresh
            secondTarget.AddIndexAsync(_testModels[2],_testModels[2].Key).Wait();
            secondTarget.FlushAsync().Wait();

            Assert.AreEqual(2, _target.Query.Count(), "Unexpected key count in original collection.");
            _target.RefreshAsync().Wait();
            Assert.AreEqual(3, _target.Query.Count(), "Refresh failed.");

        }
    }
}
