
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

using System.Linq;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("Purge")]
#endif
    [TestClass]
    public class TestPurgeAltDriver : TestPurge
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
    [Tag("Purge")]
#endif
    [TestClass]
    public class TestPurge : TestBase
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