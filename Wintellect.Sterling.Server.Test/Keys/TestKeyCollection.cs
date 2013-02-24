
#if NETFX_CORE
using Wintellect.Sterling.WinRT.WindowsStorage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#elif SILVERLIGHT
using Microsoft.Phone.Testing;
using Wintellect.Sterling.WP8.IsolatedStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using System;
using Wintellect.Sterling.Server.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System.Linq;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Keys;
using Wintellect.Sterling.Core.Serialization;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Keys
{
#if SILVERLIGHT
    [Tag("KeyCollection")]
#endif
    [Ignore]
    [TestClass]
    public class TestKeyCollectionAltDriver : TestKeyCollection
    {
        protected override ISterlingDriver GetDriver( string test )
        {
#if NETFX_CORE
            return new WindowsStorageDriver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#elif SILVERLIGHT
            return new IsolatedStorageDriver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#else
            return new FileSystemDriver( test, new DefaultSerializer(), ( lvl, msg, ex ) => { } );
#endif
        }
    }

#if SILVERLIGHT
    [Tag("KeyCollection")]
#endif
    [Ignore]
    [TestClass]
    public class TestKeyCollection : TestBase
    {
        private readonly TestModel[] _models = new[]
                                          {
                                              TestModel.MakeTestModel(), TestModel.MakeTestModel(),
                                              TestModel.MakeTestModel()
                                          };

        private ISterlingDriver _driver;
        private KeyCollection<TestModel, int> _target;
        protected readonly ISterlingDatabaseInstance _testDatabase = new TestDatabaseInterfaceInstance();
        private int _testAccessCount;

        public TestContext TestContext { get; set; }

        /// <summary>
        ///     Fetcher - also flag the fetch
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The model</returns>
        private TestModel _GetTestModelByKey(int key)
        {
            _testAccessCount++;
            return (from t in _models where t.Key.Equals(key) select t).FirstOrDefault();
        }

        [TestInitialize]
        public void TestInit()
        {
            _driver = GetDriver( TestContext.TestName );
            _testAccessCount = 0;            
            _target = new KeyCollection<TestModel, int>(_driver,
                                                        _GetTestModelByKey);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _driver.PurgeAsync().Wait();
            _driver = null;
        }

        [TestMethod]
        public void TestAddKey()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            Assert.AreEqual(0,_target.NextKey, "Next key is incorrect.");
            _target.AddKeyAsync(_models[0].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(1, _target.NextKey, "Next key not advanced.");
        }

        [TestMethod]
        public void TestAddDuplicateKey()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            Assert.AreEqual(0,_target.NextKey, "Next key is incorrect initialized.");
            _target.AddKeyAsync(_models[0].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(1, _target.NextKey, "Next key not advanced.");
            _target.AddKeyAsync(_models[0].Key).Wait();
            Assert.AreEqual(1, _target.NextKey, "Next key advanced on duplicate add."); 
            Assert.AreEqual(1, _target.Query.Count(), "Key list count is incorrect.");
        }
        
        [TestMethod]
        public void TestRemoveKey()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            Assert.AreEqual(0, _target.NextKey, "Next key is incorrect.");
            _target.AddKeyAsync(_models[0].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(1, _target.NextKey, "Next key not advanced.");       
            _target.RemoveKeyAsync(_models[0].Key).Wait();
            Assert.AreEqual(0, _target.Query.Count(), "Key was not removed.");
        }

        [TestMethod]
        public void TestQueryable()
        {
            _target.AddKeyAsync(_models[0].Key).Wait();
            _target.AddKeyAsync(_models[1].Key).Wait();
            _target.AddKeyAsync(_models[2].Key).Wait();
            Assert.AreEqual(3, _target.Query.Count(), "Key count is incorrect.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testKey = (from k in _target.Query where k.Key.Equals(_models[1].Key) select k).FirstOrDefault();
            Assert.IsNotNull(testKey, "Test key not retrieved.");
            Assert.AreEqual(_models[1].Key, testKey.Key, "Key mismatch.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testModel = testKey.LazyValue.Value; 
            Assert.AreSame(_models[1], testModel, "Model does not match.");
            Assert.AreEqual(1, _testAccessCount, "Lazy loader access count is incorrect.");
            
        }
         
        [TestMethod]
        public void TestSerialization()
        {
            _target.AddKeyAsync(_models[0].Key).Wait();
            _target.AddKeyAsync(_models[1].Key).Wait();
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set.");
            _target.FlushAsync().Wait();
            Assert.IsFalse(_target.IsDirty, "Dirty flag not reset on flush.");

            var secondTarget = new KeyCollection<TestModel, int>(_driver,
                                                                 _GetTestModelByKey);

            // are we able to grab things?
            Assert.AreEqual(2, secondTarget.Query.Count(), "Key count is incorrect.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testKey = (from k in secondTarget.Query where k.Key.Equals(_models[1].Key) select k).FirstOrDefault();
            Assert.IsNotNull(testKey, "Test key not retrieved.");
            Assert.AreEqual(_models[1].Key, testKey.Key, "Key mismatch.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testModel = testKey.LazyValue.Value;
            Assert.AreSame(_models[1], testModel, "Model does not match.");
            Assert.AreEqual(1, _testAccessCount, "Lazy loader access count is incorrect.");

            // now let's test refresh
            secondTarget.AddKeyAsync(_models[2].Key).Wait();
            secondTarget.FlushAsync().Wait();

            Assert.AreEqual(2, _target.Query.Count(), "Unexpected key count in original collection.");
            _target.RefreshAsync().Wait();
            Assert.AreEqual(3, _target.Query.Count(), "Refresh failed.");
            
        }
    }
}
