using System;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestAggregateModel
    {
        public string Key { get; set; }

        public TestModel TestModelInstance { get; set; }
        public TestForeignModel TestForeignInstance { get; set; }
        public TestBaseClassModel TestBaseClassInstance { get; set; }

        public static TestAggregateModel MakeAggregateModel()
        {
            return new TestAggregateModel
                       {
                           Key = Guid.NewGuid().ToString(),
                           TestModelInstance = TestModel.MakeTestModel(),
                           TestForeignInstance = TestForeignModel.MakeForeignModel(),
                           TestBaseClassInstance = new TestDerivedClassAModel()
                       };
        }
    }
}
