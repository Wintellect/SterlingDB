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
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("IndexQuery")]
#endif
    [TestClass]
    public class TestIndexQuery
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;
        private List<TestModel> _modelList;

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>();
            _modelList = new List<TestModel>();
            for (var i = 0; i < 10; i++)
            {
                _modelList.Add(TestModel.MakeTestModel());
                _databaseInstance.Save(_modelList[i]);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.Purge();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestSequentialQuery()
        {
            // set up queries
            var sequential = from k in _databaseInstance.Query<TestModel, string, int>(TestDatabaseInstance.DATAINDEX) orderby k.Index select k.Key;

            _modelList.Sort((m1,m2)=>m1.Data.CompareTo(m2.Data));

            var idx = 0;
            foreach (var key in sequential)
            {
                Assert.AreEqual(_modelList[idx++].Key, key, "Sequential query failed: key mismatch.");
            }
            Assert.AreEqual(idx, _modelList.Count, "Error in query: wrong number of rows.");
        }

        [TestMethod]
        public void TestDescendingQuery()
        {
            var descending = from k in _databaseInstance.Query<TestModel, string, int>(TestDatabaseInstance.DATAINDEX) orderby k.Index descending select k.Key;

            _modelList.Sort((m1,m2)=>m2.Data.CompareTo(m1.Data));

            var idx = 0;
            foreach (var key in descending)
            {
                Assert.AreEqual(_modelList[idx++].Key, key, "Descending query failed: key mismatch.");
            }
            Assert.AreEqual(idx, _modelList.Count, "Error in query: wrong number of rows.");
        }        

        [TestMethod]
        public void TestUnrolledQuery()
        {
            _modelList.Sort((m1, m2) => m1.Date.CompareTo(m2.Date));
            var unrolled = from k in _databaseInstance.Query<TestModel, string, int>(TestDatabaseInstance.DATAINDEX) 
                           orderby k.LazyValue.Value.Date select k.LazyValue.Value;

            var idx = 0;

            foreach (var model in unrolled)
            {
                Assert.AreEqual(_modelList[idx].Key, model.Key, "Unrolled query failed: key mismatch.");
                Assert.AreEqual(_modelList[idx].Date, model.Date, "Unrolled query failed: date mismatch.");
                Assert.AreEqual(_modelList[idx].Data, model.Data, "Unrolled query failed: data mismatch.");
                idx++;
            }
        }
    }
}
