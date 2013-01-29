
#if SILVERLIGHT
using Microsoft.Phone.Testing;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
    /// <summary>
    ///     Test activation-related database steps
    /// </summary>
#if SILVERLIGHT 
    [Tag("Database")]
    [Tag("Activation")]
#endif
    [TestClass]
    public class TestActivation
    {        
        /// <summary>
        ///     Test for a duplicate activation using different scenarios
        /// </summary>
        [TestMethod][Timeout(1000)]
        public void TestDuplicateActivation()
        {
            var engine1 = Factory.NewEngine();
            var engine2 = Factory.NewEngine();
            
            Assert.AreSame(engine1.SterlingDatabase, engine2.SterlingDatabase, "Sterling did not return the same database.");
            
            engine1.Activate();

            var exception = false;

            try
            {
                engine2.Activate();
            }
            catch(SterlingActivationException)
            {
                exception = true;
            }

            Assert.IsTrue(exception, "Sterling did not throw an activation exception on duplicate activation.");

            engine1.Dispose();

            // this should be ok now
            engine2.Activate();

            // now we'll duplicate it again
            exception = false;

            try
            {
                engine2.Activate();
            }
            catch (SterlingActivationException)
            {
                exception = true;
            }

            Assert.IsTrue(exception, "Sterling did not throw an activation exception on duplicate activation.");

            engine2.Dispose();            
        }

        [TestMethod][Timeout(1000)]
        public void TestDuplicateClass()
        {
            using (var engine = Factory.NewEngine())
            {
                engine.Activate();

                // now cheat and try to make a new sterling database directly
                var database = new SterlingDatabase(SterlingFactory.GetLogger());

                var exception = false; 
                try
                {
                    database.Activate();
                }
                catch (SterlingActivationException)
                {
                    exception = true;
                }

                Assert.IsTrue(exception, "Sterling did not throw an activation exception on duplicate activation with new database class.");
            }
        }

        [TestMethod][Timeout(1000)]
        public void TestActivationNotReady()
        {
            using (var engine = Factory.NewEngine())
            {
                var exception = false;

                try
                {
                    engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>();
                }
                catch (SterlingNotReadyException)
                {
                    exception = true;
                }

                Assert.IsTrue(exception, "Sterling did not throw a not ready exception on premature access.");

                engine.Activate();

                // this should not fail
                engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>();              
            }
        }
    }
}
