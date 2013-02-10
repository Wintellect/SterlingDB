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
    [Tag("Purge")]
#endif
    [TestClass]
    public class TestPurge
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

        [TestMethod]
        public void TestPurgeAction()
        {
            // save a few objects
            var sample = TestModel.MakeTestModel();
            _databaseInstance.SaveAsync( sample ).Wait();
            _databaseInstance.SaveAsync( TestModel.MakeTestModel() ).Wait();
            _databaseInstance.SaveAsync( TestModel.MakeTestModel() ).Wait();

            _databaseInstance.PurgeAsync().Wait();

            // query should be empty
            Assert.IsFalse(_databaseInstance.Query<TestModel, int>().Any(), "Purge failed: key list still exists.");

            // load should be empty
            var actual = _databaseInstance.LoadAsync<TestModel>( sample.Key ).Result;

            Assert.IsNull(actual, "Purge failed: was able to load item.");
        }
    }
}