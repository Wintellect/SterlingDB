using System;
using System.Collections.Generic;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestClassWithStruct
    {
        private static int _key = 1;

        public TestClassWithStruct()
        {
            Structs = new List<TestStruct>();
        }

        public int ID { get; set; }
        public List<TestStruct> Structs { get; set; }

        public static TestClassWithStruct MakeTestClassWithStruct()
        {
            var retVal = new TestClassWithStruct {ID = _key++};
            retVal.Structs.Add(new TestStruct { Date=DateTime.Now, Value = new Random().Next()});
            return retVal;
        }
    }
}
