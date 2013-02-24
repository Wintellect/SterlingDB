
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

using System;
using System.Collections.Generic;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Keys
{
#if SILVERLIGHT
    [Tag("CompositeKey")]
#endif
    [TestClass]
    public class TestCompositeKeyWithKeyAltDriver : TestCompositeKey
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
    [Tag("CompositeKey")]
#endif
    [TestClass]
    public class TestCompositeKeyWithKeyClass : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {           
            _engine = Factory.NewEngine();
            _engine.SterlingDatabase.RegisterSerializer<TestCompositeSerializer>();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstanceComposite>( GetDriver( TestContext.TestName ) );
            _databaseInstance.PurgeAsync().Wait();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;            
        }       

        [TestMethod]
        public void TestSave()
        {
            var random = new Random();
            // test saving and reloading
            var list = new List<TestCompositeClass>();
            for (var x = 0; x < 100; x++)
            {
                var testClass = new TestCompositeClass
                {
                    Key1 = random.Next(),
                    Key2 = random.Next().ToString(),
                    Key3 = Guid.NewGuid(),
                    Key4 = DateTime.Now.AddMinutes(-1 * random.Next(100)),
                    Data = Guid.NewGuid().ToString()
                };
                list.Add(testClass);
                _databaseInstance.SaveAsync( testClass ).Wait();
            }

            for (var x = 0; x < 100; x++)
            {
                var actual = _databaseInstance.LoadAsync<TestCompositeClass>( new TestCompositeKeyClass( list[ x ].Key1,
                    list[x].Key2,list[x].Key3,list[x].Key4)).Result;
                Assert.IsNotNull(actual, "Load failed.");
                Assert.AreEqual(list[x].Data, actual.Data, "Load failed: data mismatch.");
            }
        }
    }
}