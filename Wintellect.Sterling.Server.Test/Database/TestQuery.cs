
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
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("Query")]
#endif
    [TestClass]
    public class TestQueryAltDriver : TestQuery
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
    [Tag("Query")]
#endif
    [TestClass]
    public class TestQuery : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;
        private List<TestModel> _modelList;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>( GetDriver( TestContext.TestName ) );
            _modelList = new List<TestModel>();
            for (var i = 0; i < 10; i++)
            {
                _modelList.Add(TestModel.MakeTestModel());
                _databaseInstance.SaveAsync(_modelList[i]).Wait();
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestSequentialQuery()
        {           
            // set up queries
            var sequential = from k in _databaseInstance.Query<TestModel, int>() orderby k.Key select k.Key;

            var idx = 0;
            foreach (var key in sequential)
            {
                Assert.AreEqual(_modelList[idx++].Key, key, "Sequential query failed: key mismatch.");
            }
        }

        [TestMethod]
        public void TestDescendingQuery()
        {             
            var descending = from k in _databaseInstance.Query<TestModel, int>() orderby k.Key descending select k.Key;
                      
            var idx = _modelList.Count - 1;
            foreach (var key in descending)
            {
                Assert.AreEqual(_modelList[idx--].Key, key, "Descending query failed: key mismatch.");
            }                   
        }

        [TestMethod]
        public void TestRangeQuery()
        {           
            var range = from k in _databaseInstance.Query<TestModel, int>()
                        where k.Key > _modelList[2].Key && k.Key < _modelList[5].Key
                        orderby k.Key
                        select k.Key;

            var idx = 3;
            foreach (var key in range)
            {
                Assert.AreEqual(_modelList[idx++].Key, key, "Range query failed: key mismatch.");
            }
        }

        [TestMethod]
        public void TestUnrolledQuery()
        {            
            _modelList.Sort((m1, m2) => m1.Data.CompareTo(m2.Data));
            var unrolled = from k in _databaseInstance.Query<TestModel, int>() orderby k.LazyValue.Value.Data select k.LazyValue.Value;

            var idx = 0;

            foreach(var model in unrolled)
            {
                Assert.AreEqual(_modelList[idx].Key, model.Key, "Unrolled query failed: key mismatch.");
                Assert.AreEqual(_modelList[idx].Date, model.Date, "Unrolled query failed: date mismatch.");
                Assert.AreEqual(_modelList[idx].Data, model.Data, "Unrolled query failed: data mismatch.");
                idx++;
            }
        }
    }
}
