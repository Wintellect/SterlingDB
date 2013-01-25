using System.Collections.Generic;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestAggregateListModel
    {
        private static int _nextId = 0;

        public TestAggregateListModel()
        {
        }

        public List<TestBaseClassModel> Children { get; set; }
        public int ID { get; set; }

        public static TestAggregateListModel MakeTestAggregateListModel()
        {
            return new TestAggregateListModel()
            {
                ID = _nextId++,
                Children = new List<TestBaseClassModel>()
                {
                    new TestDerivedClassAModel(),
                    new TestDerivedClassBModel(),
                    new TestDerivedClassAModel()
                }
            };
        }
    }
}
