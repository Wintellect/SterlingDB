
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
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Database
{
    public class TestObjectField
    {
        public int Key;
        public string Data;
    }

    public class TestObjectFieldDatabase : BaseDatabaseInstance
    {
        public override string Name
        {
            get { return "TestObjectFieldDatabase"; }
        }

        protected override System.Collections.Generic.List<ITableDefinition> RegisterTables()
        {
            return new System.Collections.Generic.List<ITableDefinition>
            {
                CreateTableDefinition<TestObjectField,int>(dataDefinition => dataDefinition.Key)
            };
        }
    }

#if SILVERLIGHT
    [Tag("Field")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestFieldAltDriver : TestField
    {
        protected override ISterlingDriver GetDriver()
        {
#if NETFX_CORE
            return new WindowsStorageDriver();
#elif SILVERLIGHT
            return new IsolatedStorageDriver();
#else
            return new FileSystemDriver();
#endif
        }
    }

#if SILVERLIGHT 
    [Tag("Field")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestField : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        [TestInitialize]
        public void TestInit()
        {            
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestObjectFieldDatabase>( GetDriver() );
            _databaseInstance.PurgeAsync().Wait();
        }

        [TestMethod]
        public void TestData()
        {
            var testNull = new TestObjectField {Key = 1, Data = "data"};

            _databaseInstance.SaveAsync( testNull ).Wait();

            var loadedTestNull = _databaseInstance.LoadAsync<TestObjectField>( 1 ).Result;

            // The values in the deserialized class should be populated.
            Assert.IsNotNull(loadedTestNull);
            Assert.IsNotNull(loadedTestNull.Data);
            Assert.IsNotNull(loadedTestNull.Key);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

    }

}