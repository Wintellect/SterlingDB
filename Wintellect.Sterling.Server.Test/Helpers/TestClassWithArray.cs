using System;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestClassWithArray
    {
        private static int _id;

        public int ID { get; set; }
        public int[] ValueTypeArray { get; set; }
        public TestBaseClassModel[] BaseClassArray { get; set; }
        public TestModel[] ClassArray { get; set; }

        public static TestClassWithArray MakeTestClassWithArray()
        {
            return new TestClassWithArray()
            {
                ID = _id++,
                ValueTypeArray = new int[] { 1, 2, 3 },
                BaseClassArray = new TestBaseClassModel[] { new TestDerivedClassAModel(), new TestDerivedClassBModel() },
                ClassArray = new TestModel[] { TestModel.MakeTestModel() }
            };
        }
    }
}
