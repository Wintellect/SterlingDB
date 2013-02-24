using System;
using System.IO;
#if SILVERLIGHT
using Microsoft.Phone.Testing;
using Wintellect.Sterling.WP8;
#elif !NETFX_CORE
using Wintellect.Sterling.Server;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Wintellect.Sterling.WinRT;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Test.Serializer
{
    /// <summary>
    ///     Default serializer test
    /// </summary>
#if SILVERLIGHT    
    [Tag("Serializer")]
#endif
    [TestClass]
    public class TestExtendedSerializer
    {
        /// <summary>
        ///     The target default serializer
        /// </summary>
        private ISterlingSerializer _target;

        // test data
        const decimal DECIMAL = (decimal)5.11;
        private readonly DateTime _date = DateTime.MinValue;
        private readonly DateTime _secondDate = DateTime.Now;
        private readonly Uri _uri = new Uri("http://sterling.codeplex.com", UriKind.Absolute);
        private readonly Guid _guid = Guid.NewGuid();
        private readonly TimeSpan _timeSpan = TimeSpan.FromSeconds(2);

        [TestInitialize]
        public void Init()
        {
            _target = new ExtendedSerializer( new PlatformAdapter() );
        }

        /// <summary>
        ///     Check that serialization checks are working
        /// </summary>
        [TestMethod]
        public void TestSerializationChecks()
        {
            Assert.IsTrue(_target.CanSerialize<decimal>(), "Failed to recognize decimal.");          
            Assert.IsTrue(_target.CanSerialize<DateTime>(), "Failed to recognize date time.");
            Assert.IsTrue(_target.CanSerialize<Uri>(), "Failed to recognize uri.");
            Assert.IsTrue(_target.CanSerialize<Guid>(), "Failed to recognize guid.");
            Assert.IsTrue(_target.CanSerialize<TimeSpan>(), "Failed to recognize timespan.");
        }        

        /// <summary>
        ///     Test the serialization and deserialization
        /// </summary>
        [TestMethod]
        public void TestSerialization()
        {
            
            decimal decimalTest;
            DateTime dateTest, date2Test;
            Uri uriTest;
            Guid guidTest;
            TimeSpan timeSpanTest;

            using (var mem = new MemoryStream())
            using ( var bw = new BinaryWriter(mem) )
            {
                _target.Serialize(DECIMAL, bw);
                _target.Serialize(_date, bw);
                _target.Serialize(_secondDate, bw);
                _target.Serialize(_uri, bw);
                _target.Serialize(_guid, bw);
                _target.Serialize(_timeSpan, bw);

                mem.Seek(0, SeekOrigin.Begin);

                using (var br = new BinaryReader(mem))
                {
                    decimalTest = _target.Deserialize<decimal>(br);
                    dateTest = _target.Deserialize<DateTime>(br);
                    date2Test = _target.Deserialize<DateTime>(br);
                    uriTest = _target.Deserialize<Uri>(br);
                    guidTest = _target.Deserialize<Guid>(br);
                    timeSpanTest = (TimeSpan)_target.Deserialize(typeof(TimeSpan), br);
                }
            }

            Assert.AreEqual(DECIMAL, decimalTest, "Decimal did not deserialize correctly.");
            Assert.AreEqual(_date, dateTest, "DateTime did not deserialize correctly.");
            Assert.AreEqual(_secondDate, date2Test, "Second DateTime did not deserialize correctly.");
            Assert.AreEqual(_uri, uriTest, "Uri did not deserialize correctly.");            
            Assert.AreEqual(_guid, guidTest, "Guid did not de-serialized correctly.");
            Assert.AreEqual(_timeSpan, timeSpanTest, "Time span did not deserialize correctly.");
        }
    }
}
