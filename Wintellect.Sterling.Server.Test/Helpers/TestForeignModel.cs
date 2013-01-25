using System;

namespace Wintellect.Sterling.Test.Helpers
{
    public class TestForeignModel
    {
        /// <summary>
        ///     Key
        /// </summary>
        public Guid Key { get; set; }

        /// <summary>
        ///     Data
        /// </summary>
        public string Data { get; set; }

        public static TestForeignModel MakeForeignModel()
        {
            return new TestForeignModel {Key = Guid.NewGuid(), Data = Guid.NewGuid().ToString()};
        }
    }
}
