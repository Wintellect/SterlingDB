
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

using System.IO;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("Backup")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestBackupRestoreAltDriver : TestBackupRestore
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
    [Tag("Backup")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestBackupRestore : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod]
        public void TestBackupAndRestore()
        {
            var driver = GetDriver( TestContext.TestName );

            // activate the engine and store the data
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(driver);

            // test saving and reloading
            var expected = TestModel.MakeTestModel();

            _databaseInstance.SaveAsync(expected).Wait();

            // now back it up
            var memStream = new MemoryStream();

            byte[] databaseBuffer;

            using (var binaryWriter = new BinaryWriter(memStream))
            {
                _engine.SterlingDatabase.BackupAsync<TestDatabaseInstance>(binaryWriter).Wait();
                binaryWriter.Flush();
                databaseBuffer = memStream.ToArray();
            }

            // now purge the database
            _databaseInstance.PurgeAsync().Wait();

            var actual = _databaseInstance.LoadAsync<TestModel>( expected.Key ).Result;

            // confirm the database is gone
            Assert.IsNull(actual, "Purge failed, was able to load the test model.");

            _databaseInstance = null;

            // shut it all down
            _engine.Dispose();
            _engine = null;
            
            // get a new engine
            _engine = Factory.NewEngine();
            
            // activate it and grab the database again
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(driver);

            // restore it
            _engine.SterlingDatabase.RestoreAsync<TestDatabaseInstance>(new BinaryReader(new MemoryStream(databaseBuffer))).Wait();

            _engine.Dispose();
            _engine = null;

            // get a new engine
            _engine = Factory.NewEngine();

            // activate it and grab the database again
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(driver);            

            actual = _databaseInstance.LoadAsync<TestModel>(expected.Key).Result;

            Assert.IsNotNull(actual, "Load failed.");

            Assert.AreEqual(expected.Key, actual.Key, "Load failed: key mismatch.");
            Assert.AreEqual(expected.Data, actual.Data, "Load failed: data mismatch.");
            Assert.IsNull(actual.Data2, "Load failed: suppressed data property not valid on de-serialize.");
            Assert.IsNotNull(actual.SubClass, "Load failed: sub class is null.");
            Assert.IsNull(actual.SubClass2, "Load failed: supressed sub class should be null.");
            Assert.AreEqual(expected.SubClass.NestedText, actual.SubClass.NestedText,
                            "Load failed: sub class text mismtach.");
            Assert.AreEqual(expected.SubStruct.NestedId, actual.SubStruct.NestedId,
                            "Load failed: sub struct id mismtach.");
            Assert.AreEqual(expected.SubStruct.NestedString, actual.SubStruct.NestedString,
                            "Load failed: sub class string mismtach.");
        }
    }
}