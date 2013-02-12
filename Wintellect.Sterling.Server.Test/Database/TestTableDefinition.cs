
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

using System;
using System.Linq;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Serialization;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("TableDefinition")]
#endif
    [TestClass]
    public class TestTableDefinitionAltDriver : TestTableDefinition
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

        protected override ISterlingDriver GetDriver( string databaseName, ISterlingSerializer serializer,
                                                      Action<SterlingLogLevel, string, Exception> log )
        {
#if NETFX_CORE
            return new WindowsStorageDriver( databaseName, serializer, log );
#elif SILVERLIGHT
            return new IsolatedStorageDriver( databaseName, serializer, log );
#else
            return new FileSystemDriver( databaseName, serializer, log );
#endif
        }
    }

#if SILVERLIGHT
    [Tag("TableDefinition")]
#endif
    [TestClass]
    public class TestTableDefinition : TestBase
    {
        protected virtual ISterlingDriver GetDriver( string databaseName, ISterlingSerializer serializer,
                                                     Action<SterlingLogLevel, string, Exception> log )
        {
            return new MemoryDriver( databaseName, serializer, log );
        }

        private readonly TestModel[] _models = new[]
                                          {
                                              TestModel.MakeTestModel(), TestModel.MakeTestModel(),
                                              TestModel.MakeTestModel()
                                          };

        private TableDefinition<TestModel, int> _target;
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
            return (from t in _models where t.Key.Equals(key) select t).FirstOrDefault();
        }

        [TestInitialize]
        public void TestInit()
        {
            var serializer = new AggregateSerializer();
            serializer.AddSerializer(new DefaultSerializer());
            serializer.AddSerializer(new ExtendedSerializer());
            _testAccessCount = 0;
            _target = new TableDefinition<TestModel, int>(GetDriver(_testDatabase.Name, serializer, SterlingFactory.GetLogger().Log),
                                                        _GetTestModelByKey, t => t.Key);
        }        

        [TestMethod]
        public void TestConstruction()
        {
            Assert.AreEqual(typeof(TestModel), _target.TableType, "Table type mismatch.");
            Assert.AreEqual(typeof(int), _target.KeyType, "Key type mismatch.");
            var key = _target.FetchKey(_models[1]);
            Assert.AreEqual(_models[1].Key, key, "Key mismatch after fetch key invoked.");
        }
    }
}
