using System;
using System.Collections.Generic;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestClassWithDictionary
    {
        private static int _id = 0;

        public int ID { get; set; }
        public Dictionary<int, int> BaseDictionary { get; set; }
        public Dictionary<int, TestModel> DictionaryWithClassAsValue { get; set; }
        public Dictionary<int, TestBaseClassModel> DictionaryWithBaseClassAsValue { get; set; }
        public Dictionary<int, List<TestModel>> DictionaryWithListAsValue { get; set; }

        public static TestClassWithDictionary MakeTestClassWithDictionary()
        {
            return new TestClassWithDictionary()
            {
                ID = _id++,
                BaseDictionary = new Dictionary<int, int>()
                {
                    { 1, 2 },
                    { 2, 3 }
                },
                DictionaryWithBaseClassAsValue = new Dictionary<int, TestBaseClassModel>()
                {
                    { 1, new TestDerivedClassAModel() },
                    { 2, new TestDerivedClassBModel() }
                },
                DictionaryWithClassAsValue = new Dictionary<int, TestModel>()
                {
                    { 1, TestModel.MakeTestModel() },
                    { 2, TestModel.MakeTestModel() }
                },
                DictionaryWithListAsValue = new Dictionary<int, List<TestModel>>()
                {
                    { 1, new List<TestModel>() { TestModel.MakeTestModel(), TestModel.MakeTestModel() } }
                }
            };
        }
    }
}
