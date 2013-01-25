
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
using Wintellect.Sterling.Core.Indexes;
using Wintellect.Sterling.Core.Serialization;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Indexes
{
    [TestClass]
    public class TestSingleIndex
    {
        private MemoryDriver _driver;

        private IndexCollection<TestModel, string, int> _target;

        private List<TestModel> _testModels;

        private readonly ISterlingDatabaseInstance _testDatabase = new TestDatabaseInterfaceInstance();
        
        private int _testAccessCount;

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

        [TestInitialize]
        public void Init()
        {
            if (_driver == null)
            {
                _driver = new MemoryDriver(_testDatabase.Name, new DefaultSerializer(), SterlingFactory.GetLogger().Log);
            }
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
        }

        [TestMethod]
        public void TestAddIndex()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            _target.AddIndex(_testModels[0], _testModels[0].Key);
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");            
        }

        [TestMethod]
        public void TestAddDuplicateIndex()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            _target.AddIndex(_testModels[0], _testModels[0].Key);
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(_target.Query.Count(),1, "Index count is incorrect.");
            _target.AddIndex(_testModels[0], _testModels[0].Key);
            Assert.AreEqual(_target.Query.Count(), 1, "Index count is incorrect.");            
        }

        [TestMethod]
        public void TestRemoveIndex()
        {
            Assert.IsFalse(_target.IsDirty, "Dirty flag set prematurely");
            _target.AddIndex(_testModels[0], _testModels[0].Key);
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set on add.");
            Assert.AreEqual(1, _target.Query.Count(), "Index count is incorrect.");
            _target.RemoveIndex(_testModels[0].Key);
            Assert.AreEqual(0, _target.Query.Count(), "Index was not removed.");
        }

        [TestMethod]
        public void TestQueryable()
        {
            _target.AddIndex(_testModels[0],_testModels[0].Key);
            _target.AddIndex(_testModels[1], _testModels[1].Key);
            _target.AddIndex(_testModels[2], _testModels[2].Key);
            Assert.AreEqual(3, _target.Query.Count(), "Key count is incorrect.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testIndex = (from k in _target.Query where k.Index.Equals(_testModels[1].Data) select k).FirstOrDefault();
            Assert.IsNotNull(testIndex, "Test key not retrieved.");
            Assert.AreEqual(_testModels[1].Key, testIndex.Key, "Key mismatch.");
            Assert.AreEqual(0, _testAccessCount, "Lazy loader was accessed prematurely.");
            var testModel = testIndex.LazyValue.Value;
            Assert.AreSame(_testModels[1], testModel, "Model does not match.");
            Assert.AreEqual(1, _testAccessCount, "Lazy loader access count is incorrect.");
        }

        [TestMethod]
        public void TestSerialization()
        {
            _target.AddIndex(_testModels[0], _testModels[0].Key);
            _target.AddIndex(_testModels[1], _testModels[1].Key);            
            Assert.IsTrue(_target.IsDirty, "Dirty flag not set.");
            _target.Flush();
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
            var testModel = testIndex.LazyValue.Value;
            Assert.AreSame(_testModels[1], testModel, "Model does not match.");
            Assert.AreEqual(1, _testAccessCount, "Lazy loader access count is incorrect.");

            // now let's test refresh
            secondTarget.AddIndex(_testModels[2],_testModels[2].Key);
            secondTarget.Flush();

            Assert.AreEqual(2, _target.Query.Count(), "Unexpected key count in original collection.");
            _target.Refresh();
            Assert.AreEqual(3, _target.Query.Count(), "Refresh failed.");

        }
    }
}
