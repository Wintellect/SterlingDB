#if SILVERLIGHT
using Microsoft.Phone.Testing;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("DatabaseRegistration")]
#endif
    [TestClass]
    public class TestRegistration
    {        
        [TestMethod]
        public void TestDatabaseRegistration()
        {
            using (var engine = Factory.NewEngine())
            {
                var db = engine.SterlingDatabase;

                // test not activated yet 
                var raiseError = false;

                try
                {
                    db.RegisterDatabase<TestDatabaseInstance>( "register" );
                }
                catch(SterlingNotReadyException)
                {
                    raiseError = true;
                }

                Assert.IsTrue(raiseError, "Sterling did not throw activation error.");

                engine.Activate();

                var testDb2 = db.RegisterDatabase<TestDatabaseInstance>( "register" );

                Assert.IsNotNull(testDb2, "Database registration returned null.");
                Assert.IsInstanceOfType(testDb2, typeof(TestDatabaseInstance), "Incorrect database type returned.");
            
                Assert.AreEqual("register", testDb2.Name, "Incorrect database name.");

                // test bad database (no table definitions) 
                raiseError = false;

                try
                {
                    db.RegisterDatabase<DupDatabaseInstance>( "register" );
                }
                catch (SterlingDuplicateTypeException)
                {
                    raiseError = true;
                }

                Assert.IsTrue(raiseError, "Sterling did not catch the duplicate type registration.");
            }
        }
    }
}
