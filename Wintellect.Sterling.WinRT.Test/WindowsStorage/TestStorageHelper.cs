
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

using Wintellect.Sterling.WinRT.WindowsStorage;

namespace Wintellect.Sterling.Test.WindowsStorage
{
    [TestClass]
    public class TestStorageHelper
    {
        [DataContract]
        private class MyClass
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public int Age { get; set; }
        }

        [TestMethod]
        public void TestReadWriteBinaryFile()
        {
            var obj = new MyClass { Name = "Joe", Age = 87 };
            var json = StorageHelper.Serialize( obj );

            using ( var writer = StorageHelper.GetWriterForFileAsync( "joe.bin" ).Result )
            {
                writer.Write( json );
            }

            MyClass obj2 = null;

            using ( var reader = StorageHelper.GetReaderForFileAsync( "joe.bin" ).Result )
            {
                var json2 = reader.ReadString();
                obj2 = StorageHelper.Deserialize<MyClass>( json2 );
            }

            Assert.AreEqual( obj.Name, obj2.Name );
            Assert.AreEqual( obj.Age, obj2.Age );
        }

        [TestMethod]
        public void TestEnsureFolderOneLevel()
        {
            StorageHelper.EnsureFolderExistsAsync( "one" ).Wait();

            var one = StorageHelper.GetFolderAsync( "one" ).Result;

            Assert.AreEqual( "one", one.Name );
        }

        [TestMethod]
        public void TestEnsureFolderMultipleLevel()
        {
            StorageHelper.EnsureFolderExistsAsync( "/one/two/three/four" ).Wait();

            var four = StorageHelper.GetFolderAsync( "/one/two/three/four" ).Result;

            Assert.AreEqual( "four", four.Name );
        }
    }
}
