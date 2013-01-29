using System.IO;
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
    [Tag("Backup")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestBackupRestore
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;
        private MemoryDriver _driver;

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }

        [TestMethod][Timeout(1000)]
        public void TestBackupAndRestore()
        {
            if (_driver == null)
            {
                _driver = new MemoryDriver();
            }

            // activate the engine and store the data
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(_driver);

            // test saving and reloading
            var expected = TestModel.MakeTestModel();

            _databaseInstance.SaveAsync(expected).Wait();

            // now back it up
            var memStream = new MemoryStream();

            byte[] databaseBuffer;

            using (var binaryWriter = new BinaryWriter(memStream))
            {
                _engine.SterlingDatabase.Backup<TestDatabaseInstance>(binaryWriter);
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
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(_driver);

            // restore it
            _engine.SterlingDatabase.Restore<TestDatabaseInstance>(new BinaryReader(new MemoryStream(databaseBuffer)));

            _engine.Dispose();
            _engine = null;

            // get a new engine
            _engine = Factory.NewEngine();

            // activate it and grab the database again
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(_driver);            

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