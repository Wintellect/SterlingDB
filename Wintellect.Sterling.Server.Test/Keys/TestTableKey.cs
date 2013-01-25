using System;
using System.Linq;
#if SILVERLIGHT
using Microsoft.Phone.Testing;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Keys;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Keys
{
#if SILVERLIGHT
    [Tag("TableKey")]
#endif
    [TestClass]
    public class TestTableKey
    {
        [TestMethod]
        public void TestTableKeyFunctionality()
        {
            var list = new[] {TestModel.MakeTestModel(), TestModel.MakeTestModel()};

            Func<int, TestModel> getter = i => (from t in list where t.Key.Equals(i) select t).FirstOrDefault();

            var key1 = new TableKey<TestModel, int>(list[0].Key, getter);
            var key2 = new TableKey<TestModel, int>(list[1].Key, getter);

            Assert.AreEqual(key1.Key, list[0].Key, "Key mismatch.");
            Assert.AreEqual(key2.Key, list[1].Key, "Key mismatch.");
            
            Assert.IsFalse(key1.LazyValue.IsValueCreated, "Lazy model already created.");
            var testModel1 = key1.LazyValue.Value;
            Assert.IsTrue(key1.LazyValue.IsValueCreated, "Lazy value created was not set.");
            Assert.AreSame(list[0], testModel1, "First key returned invalid instance.");
            Assert.AreSame(list[1], key2.LazyValue.Value, "Second key return invalid instance.");
        }

    }
}
