
using System;

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
using System.Threading;
using System.Threading.Tasks;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("Async")]
#endif 
    [TestClass]
    public class TestPendingOperation
    {
        [TestMethod]
        public void TestSimpleOperation()
        {
            int x = 0;

            var operation = new PendingOperation( token => { x = 10; } );

            operation.Task.Wait();

            Assert.AreEqual( 10, x, "Pending operation failed to execute correctly." );
        }

        [TestMethod]
        public void TestSimpleOperationWithProgress()
        {
            var x = 0;
            var prog = 0m;

            PlatformAdapter.Instance = Factory.NewPlatformAdapter();

            var operation = new PendingOperation( ( token, progress ) =>
            {
                PlatformAdapter.Instance.Sleep( 250 );
                progress( 0 );
                x = 10;
                progress( 33 );
                x = 20;
                progress( 66 );
                x = 30;
                progress( 99 );
                x = 50;
            } );

            operation.ProgressChanged += ( o, e ) => { prog = e.Progress; };

            operation.Task.Wait();

            Assert.AreEqual( 50, x, "Pending operation failed to execute correctly." );
            Assert.AreEqual( 99, prog, "Pending operation failed to execute correctly." );
        }

        [TestMethod]
        public void TestCancellation()
        {
            PlatformAdapter.Instance = Factory.NewPlatformAdapter();

            var operation = new PendingOperation( ( token, progress ) =>
            {
                PlatformAdapter.Instance.Sleep( 250 );

                var sw = new SpinWait();

                for ( var i = 0; i < 1000; i++ )
                {
                    token.ThrowIfCancellationRequested();
                    sw.SpinOnce();
                }
            } );

            var cancelEventFired = false;
            var cancelExceptionThrown = false;
            var errorOccuredEventFired = false;

            operation.Canceled += ( o, e ) => cancelEventFired = true;
            operation.ErrorOccured += ( o, e ) => errorOccuredEventFired = true;

            operation.Cancel();

            try
            {
                operation.Task.Wait();
            }
            catch ( AggregateException )
            {
                cancelExceptionThrown = true;
            }

            Assert.AreEqual( errorOccuredEventFired, false, "Pending operation failed to cancel correctly." );
            Assert.AreEqual( cancelEventFired, true, "Pending operation failed to cancel correctly." );
            Assert.AreEqual( cancelExceptionThrown, true, "Pending operation failed to cancel correctly." );
            Assert.AreEqual( operation.Task.Status, TaskStatus.Canceled, "Pending operation failed to cancel correctly." );
        }
    }
}
