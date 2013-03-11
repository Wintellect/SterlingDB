
#if NETFX_CORE
using Wintellect.Sterling.WinRT.WindowsStorage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#elif SILVERLIGHT
using Microsoft.Phone.Testing;
using Wintellect.Sterling.WP8.IsolatedStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Wintellect.Sterling.Server.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;
using Wintellect.Sterling.Core.Exceptions;
using Wintellect.Sterling.Test.Helpers;

namespace Wintellect.Sterling.Test.Database
{
#if SILVERLIGHT
    [Tag("SaveAndLoad")]
#endif
    [TestClass]
    public class TestSaveAndLoadAltDriver : TestSaveAndLoad
    {
        protected override ISterlingDriver GetDriver()
        {
#if NETFX_CORE
            return new WindowsStorageDriver();
#elif SILVERLIGHT
            return new IsolatedStorageDriver();
#elif AZURE_DRIVER
            return new Wintellect.Sterling.Server.Azure.TableStorage.Driver();
#else
            return new FileSystemDriver();
#endif
        }
    }

#if SILVERLIGHT
    [Tag("SaveAndLoad")]
#endif
    [TestClass]
    public class TestSaveAndLoad : TestBase
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _databaseInstance;

        public TestContext TestContext { get; set; }

        public class TestLateBoundTable
        {
            public int Id { get; set; }
            public string Data { get; set; }
        }

        public class TestSecondLateBoundTable : TestLateBoundTable
        {        
        }

        [TestInitialize]
        public void TestInit()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(TestContext.TestName, GetDriver());
            _databaseInstance.PurgeAsync().Wait();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _databaseInstance.PurgeAsync().Wait();
            _engine.Dispose();
            _databaseInstance = null;
        }

        [TestMethod]
        public async Task TestSaveExceptions()
        {
            var raiseException = false;
            try
            {
                await _databaseInstance.SaveAsync( this );
            }
            catch ( SterlingTableNotFoundException )
            {
                raiseException = true;
            }

            Assert.IsTrue(raiseException, "Sterling did not raise exception for unknown type.");
        }

        [TestMethod]
        public void TestSave()
        {
            // test saving and reloading
            var expected = TestModel.MakeTestModel();

            _databaseInstance.SaveAsync( expected ).Wait();

            var actual = _databaseInstance.LoadAsync<TestModel>( expected.Key ).Result;

            Assert.IsNotNull(actual, "Load failed.");

            Assert.AreEqual(expected.Key, actual.Key, "Load failed: key mismatch.");
            Assert.AreEqual(expected.Data, actual.Data, "Load failed: data mismatch.");
            Assert.IsNull(actual.Data2, "Load failed: suppressed data property not valid on de-serialize.");
            Assert.IsNotNull(actual.SubClass, "Load failed: sub class is null.");
            Assert.IsNull(actual.SubClass2, "Load failed: supressed sub class should be null.");           
            Assert.AreEqual(expected.SubClass.NestedText, actual.SubClass.NestedText, "Load failed: sub class text mismtach.");
            Assert.AreEqual(expected.SubStruct.NestedId, actual.SubStruct.NestedId, "Load failed: sub struct id mismtach.");
            Assert.AreEqual(expected.SubStruct.NestedString, actual.SubStruct.NestedString, "Load failed: sub class string mismtach.");
        }

        [TestMethod]
        [Ignore]
        public void TestSaveLateBoundTable()
        {
            // test saving and reloading
            var expected = new TestLateBoundTable {Id = 1, Data = Guid.NewGuid().ToString()};

            _databaseInstance.RegisterTableDefinition(_databaseInstance.CreateTableDefinition<TestLateBoundTable,int>(t=>t.Id));

            _databaseInstance.SaveAsync( expected ).Wait();

            var actual = _databaseInstance.LoadAsync<TestLateBoundTable>( expected.Id ).Result;

            Assert.IsNotNull(actual, "Load failed.");

            Assert.AreEqual(expected.Id, actual.Id, "Load failed: key mismatch.");
            Assert.AreEqual(expected.Data, actual.Data, "Load failed: data mismatch.");

            _databaseInstance.FlushAsync().Wait();

            _engine.Dispose();
            var driver = _databaseInstance.Driver;
            _databaseInstance = null;

            // bring it back up
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(TestContext.TestName, driver);

            // do this in a different order
            _databaseInstance.RegisterTableDefinition(_databaseInstance.CreateTableDefinition<TestSecondLateBoundTable,int>(t=>t.Id));

            _databaseInstance.RegisterTableDefinition(_databaseInstance.CreateTableDefinition<TestLateBoundTable, int>(t => t.Id));

            actual = _databaseInstance.LoadAsync<TestLateBoundTable>( expected.Id ).Result;

            Assert.IsNotNull(actual, "Load failed after restart.");

            Assert.AreEqual(expected.Id, actual.Id, "Load failed: key mismatch after restart.");
            Assert.AreEqual(expected.Data, actual.Data, "Load failed: data mismatch after restart.");
        }

        [TestMethod]
        public void TestSaveShutdownReInitialize()
        {
            _databaseInstance.PurgeAsync().Wait();

            // test saving and reloading
            var expected1 = TestModel.MakeTestModel();
            var expected2 = TestModel.MakeTestModel();

            expected2.GuidNullable = null;

            var expectedComplex = new TestComplexModel
                                      {
                                          Id = 5,
                                          Dict = new Dictionary<string, string>(),
                                          Models = new ObservableCollection<TestModel>()
                                      };
            for (var x = 0; x < 10; x++)
            {
                expectedComplex.Dict.Add(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                expectedComplex.Models.Add(TestModel.MakeTestModel());
            }

            _databaseInstance.SaveAsync( expected1 ).Wait();
            _databaseInstance.SaveAsync( expected2 ).Wait();
            _databaseInstance.SaveAsync( expectedComplex ).Wait();

            _databaseInstance.FlushAsync().Wait();
            
            // shut it down

            _engine.Dispose();
            var driver = _databaseInstance.Driver; 
            _databaseInstance = null;

            // bring it back up
            _engine = Factory.NewEngine();
            _engine.Activate();
            _databaseInstance = _engine.SterlingDatabase.RegisterDatabase<TestDatabaseInstance>(TestContext.TestName, driver);

            var actual1 = _databaseInstance.LoadAsync<TestModel>( expected1.Key ).Result;
            var actual2 = _databaseInstance.LoadAsync<TestModel>( expected2.Key ).Result;
            
            Assert.IsNotNull(actual1, "Load failed for 1.");
            Assert.AreEqual(expected1.Key, actual1.Key, "Load failed (1): key mismatch.");
            Assert.AreEqual(expected1.Data, actual1.Data, "Load failed(1): data mismatch.");
            Assert.IsNotNull(actual1.SubClass, "Load failed (1): sub class is null.");
            Assert.AreEqual(expected1.SubClass.NestedText, actual1.SubClass.NestedText, "Load failed (1): sub class text mismtach.");
            Assert.AreEqual(expected1.GuidNullable, actual1.GuidNullable, "Load failed (1): nullable Guid mismtach.");

            Assert.IsNotNull(actual2, "Load failed for 2.");
            Assert.AreEqual(expected2.Key, actual2.Key, "Load failed (2): key mismatch.");
            Assert.AreEqual(expected2.Data, actual2.Data, "Load failed (2): data mismatch.");
            Assert.IsNotNull(actual2.SubClass, "Load failed (2): sub class is null.");
            Assert.AreEqual(expected2.SubClass.NestedText, actual2.SubClass.NestedText, "Load failed (2): sub class text mismatch.");
            Assert.IsNull(expected2.GuidNullable, "Load failed (2): nullable Guid was not loaded as null.");

            //insert a third 
            var expected3 = TestModel.MakeTestModel();
            _databaseInstance.SaveAsync( expected3 ).Wait();

            actual1 = _databaseInstance.LoadAsync<TestModel>( expected1.Key ).Result;
            actual2 = _databaseInstance.LoadAsync<TestModel>( expected2.Key ).Result;
            var actual3 = _databaseInstance.LoadAsync<TestModel>( expected3.Key ).Result;

            Assert.IsNotNull(actual1, "Load failed for 1.");
            Assert.AreEqual(expected1.Key, actual1.Key, "Load failed (1): key mismatch.");
            Assert.AreEqual(expected1.Data, actual1.Data, "Load failed(1): data mismatch.");
            Assert.IsNotNull(actual1.SubClass, "Load failed (1): sub class is null.");
            Assert.AreEqual(expected1.SubClass.NestedText, actual1.SubClass.NestedText, "Load failed (1): sub class text mismtach.");

            Assert.IsNotNull(actual2, "Load failed for 2.");
            Assert.AreEqual(expected2.Key, actual2.Key, "Load failed (2): key mismatch.");
            Assert.AreEqual(expected2.Data, actual2.Data, "Load failed (2): data mismatch.");
            Assert.IsNotNull(actual2.SubClass, "Load failed (2): sub class is null.");
            Assert.AreEqual(expected2.SubClass.NestedText, actual2.SubClass.NestedText, "Load failed (2): sub class text mismtach.");

            Assert.IsNotNull(actual3, "Load failed for 3.");
            Assert.AreEqual(expected3.Key, actual3.Key, "Load failed (3): key mismatch.");
            Assert.AreEqual(expected3.Data, actual3.Data, "Load failed (3): data mismatch.");
            Assert.IsNotNull(actual3.SubClass, "Load failed (3): sub class is null.");
            Assert.AreEqual(expected3.SubClass.NestedText, actual3.SubClass.NestedText, "Load failed (3): sub class text mismtach.");

            // load the complex 
            var actualComplex = _databaseInstance.LoadAsync<TestComplexModel>( 5 ).Result;
            Assert.IsNotNull(actualComplex, "Load failed (complex): object is null.");
            Assert.AreEqual(5, actualComplex.Id, "Load failed: id mismatch.");
            Assert.IsNotNull(actualComplex.Dict, "Load failed: dictionary is null.");
            foreach(var key in expectedComplex.Dict.Keys)
            {
                var value = expectedComplex.Dict[key];
                Assert.IsTrue(actualComplex.Dict.Contains(key), "Load failed: dictionary is missing key.");
                Assert.AreEqual(value, actualComplex.Dict[key], "Load failed: dictionary has invalid value.");
            }

            Assert.IsNotNull(actualComplex.Models, "Load failed: complex missing the model collection.");

            foreach(var model in expectedComplex.Models)
            {
                var targetModel = actualComplex.Models.Where(m => m.Key.Equals(model.Key)).FirstOrDefault();
                Assert.IsNotNull(targetModel, "Load failed for nested model.");
                Assert.AreEqual(model.Key, targetModel.Key, "Load failed for nested model: key mismatch.");
                Assert.AreEqual(model.Data, targetModel.Data, "Load failed for nested model: data mismatch.");
                Assert.IsNotNull(targetModel.SubClass, "Load failed for nested model: sub class is null.");
                Assert.AreEqual(model.SubClass.NestedText, targetModel.SubClass.NestedText, "Load failed for nested model: sub class text mismtach.");
            }

        }
        
        [TestMethod]
        public void TestSaveForeign()
        {
            var expected = TestAggregateModel.MakeAggregateModel();

            _databaseInstance.SaveAsync( expected ).Wait();

            var actual = _databaseInstance.LoadAsync<TestAggregateModel>( expected.Key ).Result;
            var actualTestModel = _databaseInstance.LoadAsync<TestModel>( expected.TestModelInstance.Key ).Result;
            var actualForeignModel = _databaseInstance.LoadAsync<TestForeignModel>( expected.TestForeignInstance.Key ).Result;
            var actualDerivedModel = _databaseInstance.LoadAsync<TestDerivedClassAModel>( expected.TestBaseClassInstance.Key ).Result;

            Assert.AreEqual(expected.Key, actual.Key, "Load with foreign key failed: key mismatch.");
            Assert.AreEqual(expected.TestForeignInstance.Key, actual.TestForeignInstance.Key, "Load failed: foreign key mismatch.");
            Assert.AreEqual(expected.TestForeignInstance.Data, actual.TestForeignInstance.Data, "Load failed: foreign data mismatch.");
            Assert.AreEqual(expected.TestModelInstance.Key, actual.TestModelInstance.Key, "Load failed: test model key mismatch.");
            Assert.AreEqual(expected.TestModelInstance.Data, actual.TestModelInstance.Data, "Load failed: test model data mismatch.");
            Assert.AreEqual(expected.TestForeignInstance.Key, actualForeignModel.Key, "Load failed: foreign key mismatch on direct load.");
            Assert.AreEqual(expected.TestForeignInstance.Data, actualForeignModel.Data, "Load failed: foreign data mismatch on direct load.");
            Assert.AreEqual(expected.TestModelInstance.Key, actualTestModel.Key, "Load failed: test model key mismatch on direct load.");
            Assert.AreEqual(expected.TestModelInstance.Data, actualTestModel.Data, "Load failed: test model data mismatch on direct load.");

            Assert.AreEqual(expected.TestBaseClassInstance.Key, actual.TestBaseClassInstance.Key, "Load failed: base class key mismatch.");
            Assert.AreEqual(expected.TestBaseClassInstance.BaseProperty, actual.TestBaseClassInstance.BaseProperty, "Load failed: base class data mismatch.");
            Assert.AreEqual(expected.TestBaseClassInstance.GetType(), actual.TestBaseClassInstance.GetType(), "Load failed: base class type mismatch.");
        }

        [TestMethod]
        public void TestSaveForeignNull()
        {
            var expected = TestAggregateModel.MakeAggregateModel();
            expected.TestForeignInstance = null;

            _databaseInstance.SaveAsync( expected ).Wait();

            var actual = _databaseInstance.LoadAsync<TestAggregateModel>( expected.Key ).Result;
            var actualTestModel = _databaseInstance.LoadAsync<TestModel>( expected.TestModelInstance.Key ).Result;
            
            Assert.AreEqual(expected.Key, actual.Key, "Load with foreign key failed: key mismatch.");
            Assert.IsNull(actual.TestForeignInstance, "Load failed: foreign key not set to null.");
            Assert.AreEqual(expected.TestModelInstance.Key, actual.TestModelInstance.Key, "Load failed: test model key mismatch.");
            Assert.AreEqual(expected.TestModelInstance.Data, actual.TestModelInstance.Data, "Load failed: test model data mismatch.");
            Assert.AreEqual(expected.TestModelInstance.Key, actualTestModel.Key, "Load failed: test model key mismatch on direct load.");
            Assert.AreEqual(expected.TestModelInstance.Data, actualTestModel.Data, "Load failed: test model data mismatch on direct load.");
        }

        [TestMethod]
        public void TestSaveAsWithBase()
        {
            var expected = new TestIndexedSubclassBase();
            expected.BaseProperty = "This is base";
            expected.Id = 1;
            _databaseInstance.SaveAsAsync<TestIndexedSubclassBase>( expected ).Wait();

            var actual = _databaseInstance.LoadAsync<TestIndexedSubclassBase>( expected.Id ).Result;

            Assert.AreEqual(expected.Id, actual.Id, "Save As failed: key mismatch. ");
            Assert.AreEqual(expected.BaseProperty, actual.BaseProperty, "Save As failed: base property mismatch. ");
        }

        [TestMethod]
        public void TestSaveAsWithSubclass()
        {
            var expected = new TestIndexedSubclassModel();
            expected.BaseProperty = "This is base";
            expected.SubclassProperty = "This is subclass";
            expected.Id = 2;
            _databaseInstance.SaveAsAsync<TestIndexedSubclassBase>( expected ).Wait();

            var actual = _databaseInstance.LoadAsync<TestIndexedSubclassBase>( expected.Id ).Result;
            var actualSubclass = actual as TestIndexedSubclassModel;

            Assert.AreEqual(expected.Id, actual.Id, "Save As failed: key mismatch. ");
            Assert.AreEqual(expected.BaseProperty, actual.BaseProperty, "Save As failed: base property mismatch. ");
            Assert.IsNotNull(actualSubclass, "Save As failed: Subclass not honoured on deserialization. ");
            Assert.AreEqual(expected.SubclassProperty, actualSubclass.SubclassProperty, "Save As failed: Subclass property mismatch. ");
        }

        [TestMethod]
        public void TestSaveAsWithInvalidSubclass()
        {
            SterlingException expectedException = null;
            var expected = new TestIndexedSubclassFake();

            var expectedErrorMessage = string.Format("{0} is not of type {1}", expected.GetType().Name, typeof(TestIndexedSubclassBase).Name);
            
            expected.BaseProperty = "This is base";
            expected.SubclassProperty = "This is subclass";
            expected.Id = 2;

            try
            {
                _databaseInstance.SaveAsAsync(typeof(TestIndexedSubclassBase),expected).Wait();
            }
            catch (SterlingException ex)
            {
                expectedException = ex;
            }

            Assert.IsNotNull(expectedException, "Save As failed: succeeded with inaccurate subclass");
            Assert.IsInstanceOfType(expectedException, typeof(SterlingException));
            Assert.AreEqual(expectedErrorMessage,expectedException.Message);
        }
    }
}
