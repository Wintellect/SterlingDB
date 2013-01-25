using System;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestDerivedClassAModel : TestBaseClassModel
    {
        public String PropertyA { get; set; }

        public TestDerivedClassAModel()
            : base()
        {
            PropertyA = "Property A value";
        }
    }
}
