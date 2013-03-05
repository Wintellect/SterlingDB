
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
    [Tag("Serializers")]
#endif
    [TestClass]
    public class TestSerializersAltDriver : TestSerializers
    {
        protected override ISterlingDriver GetDriver( string test )
        {
#if NETFX_CORE
            return new WindowsStorageDriver( test );
#elif SILVERLIGHT
            return new IsolatedStorageDriver( test );
#elif AZURE_DRIVER
            return new Wintellect.Sterling.Server.Azure.TableStorage.Driver();
#else
            return new FileSystemDriver( test );
#endif
        }
    }

#if SILVERLIGHT
    [Tag("Serializers")]
#endif 
    [TestClass]
    public class TestSerializers : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.SterlingDatabase.RegisterSerializer<TestSerializer>();
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
            var expected = TestClassWithStruct.MakeTestClassWithStruct();
            var key = _databaseInstance.SaveAsync( expected ).Result;
            var actual = _databaseInstance.LoadAsync<TestClassWithStruct>(key).Result;
            Assert.IsNotNull(actual, "Save/load failed: model is null.");
            Assert.AreEqual(expected.ID, actual.ID, "Save/load failed: key mismatch.");
            Assert.IsNotNull(actual.Structs, "Save/load failed: list not initialized.");
            Assert.AreEqual(expected.Structs.Count, actual.Structs.Count, "Save/load failed: list size mismatch.");
            Assert.AreEqual(expected.Structs[0].Date, actual.Structs[0].Date, "Save/load failed: date mismatch.");
            Assert.AreEqual(expected.Structs[0].Value, actual.Structs[0].Value, "Save/load failed: value mismatch.");
        }
    }
}
