using System.Collections.Generic;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestModelAsListModel : List<TestModel>
    {
        private static int _nextId;

        public int Id { get; set; }

        public static TestModelAsListModel MakeTestModelAsList()
        {
            var model = new TestModelAsListModel
                       {
                            Id = ++_nextId
                       };
            model.AddRange(new[] { TestModel.MakeTestModel(), TestModel.MakeTestModel(), TestModel.MakeTestModel()});
            return model;           
        }

        public static TestModelAsListModel MakeTestModelAsListWithParentReference()
        {
            var model = new TestModelAsListModel
            {
                Id = ++_nextId
            };
            model.AddRange(new[] { TestModel.MakeTestModel(model), TestModel.MakeTestModel(model), TestModel.MakeTestModel(model) });
            return model;  
        }

        public static TestModelAsListModel Parent { get; set; }
    }
}
