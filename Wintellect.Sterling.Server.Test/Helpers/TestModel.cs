using System;
using System.Collections;
using System.Collections.ObjectModel;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Serialization;

namespace Wintellect.Sterling.Test.Helpers
{    
    /// <summary>
    ///     A sub class Sterling isn't explicitly aware of
    /// </summary>
    public class TestSubclass
    {
        public string NestedText { get; set; }
    }

    public struct TestSubStruct
    {
        public int NestedId;
        public string NestedString;
    }

    /// <summary>
    ///     A sub class Sterling isn't explicitly aware of
    ///     That is suppressed
    /// </summary>
    [SterlingIgnore]
    public class TestSubclass2
    {
        public string NestedText { get; set; }
    }

    /// <summary>
    ///     A test model for testing serialization
    /// </summary>
    public class TestModel
    {
        private static readonly Random _random = new Random((int) DateTime.Now.Ticks);
    
        public TestModel()
        {
            SubClass = new TestSubclass();
            Accessed = false;
        }        

        private static int _idx;

        public const int SAMPLE_CONSTANT = 2;

        /// <summary>
        ///     Determines if the class was accessed
        /// </summary>
        public bool Accessed { get; private set; }

        public void ResetAccess()
        {
            Accessed = false;
        }

        /// <summary>
        ///     The key
        /// </summary>
        public int Key { get; set; }

        private string _data;

        /// <summary>
        ///     Data
        /// </summary>
        public string Data
        {
            get
            {
                Accessed = true;
                return _data;
            }

            set { _data = value; }
        }

        public Guid? GuidNullable { get; set; }
        
        [SterlingIgnore]
        public string Data2 { get; set; }

        public TestSubclass SubClass { get; set; }

        public TestSubclass2 SubClass2 { get; set; }

        public TestSubStruct SubStruct { get; set; }

        public TestModelAsListModel Parent { get; set; }

        /// <summary>
        ///     The date
        /// </summary>
        public DateTime Date { get; set; }

        public static TestModel MakeTestModel()
        {

            return new TestModel { Data = Guid.NewGuid().ToString(), Data2 = Guid.NewGuid().ToString(), Date = DateTime.Now.AddSeconds(_random.Next()), Key = _idx++, SubClass = new TestSubclass { NestedText = Guid.NewGuid().ToString() },
                                   SubClass2 = new TestSubclass2 { NestedText = Guid.NewGuid().ToString() }, GuidNullable = Guid.NewGuid(),
                                   SubStruct = new TestSubStruct { NestedId = _idx, NestedString = Guid.NewGuid().ToString() }
            };
        }

        internal static TestModel MakeTestModel(TestModelAsListModel parentModel)
        {
            return new TestModel { Data = Guid.NewGuid().ToString(), Data2 = Guid.NewGuid().ToString(), Date = DateTime.Now.AddSeconds(_random.Next()), Key = _idx++, SubClass = new TestSubclass { NestedText = Guid.NewGuid().ToString() }, 
                SubClass2 = new TestSubclass2 { NestedText = Guid.NewGuid().ToString() }, 
                SubStruct = new TestSubStruct { NestedId = _idx, NestedString = Guid.NewGuid().ToString() },
                Parent = parentModel };
        }
    }

    public class TestComplexModel
    {
        public int Id { get; set; }
        public IDictionary Dict { get; set; }
        public ObservableCollection<TestModel> Models { get; set; }
    }

    public class TestIndexedSubclassBase
    {
        public int Id {get;set;}
        public string BaseProperty { get; set; }
    }

    public class TestIndexedSubclassModel:TestIndexedSubclassBase
    {
        public string SubclassProperty { get; set; }
    }

    public class TestIndexedSubclassFake {
        public int Id;
        public string BaseProperty { get; set; }
        public string SubclassProperty { get; set; }
    }
}
