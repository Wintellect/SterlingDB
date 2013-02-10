using System;
using System.Linq;
#if SILVERLIGHT
using Microsoft.Phone.Testing;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System.Collections.Generic;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Database
{
    public abstract class BaseNested
    {
        public Guid Id { get; set; }
    }

    public class Bill : BaseNested
    {
        public Bill()
        {
            Partakers = new List<Partaker>();
        }

        public string Name { get; set; }
        public List<Partaker> Partakers { get; set; }
        public double Total { get; set; }
    }

    public class Person : BaseNested
    {
        public string Name { get; set; }
    }

    public class Partaker : BaseNested
    {
        public double Paid { get; set; }
        public Person Person { get; set; }
    }

    public class NestedInstancesDatabase : BaseDatabaseInstance
    {       
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
        {
            CreateTableDefinition<Bill, Guid>( b => b.Id ),
            CreateTableDefinition<Person, Guid>( p => p.Id )
        };
        }
    }

#if SILVERLIGHT
    [Tag("Nested")]
    [Tag("Database")]
#endif
    [TestClass]
    public class TestNestedInstance
    {
        private SterlingEngine _engine;
        private ISterlingDatabaseInstance _database;
        private readonly ISterlingDriver _memoryDriver = new MemoryDriver();

        [TestInitialize]
        public void Init()
        {
            _engine = Factory.NewEngine();
            _engine.Activate();
            // Also fails when using memory storage, but you must remove explicit calls to Init and Shutdown
            // in the test methods.
            _database = _engine.SterlingDatabase.RegisterDatabase<NestedInstancesDatabase>(_memoryDriver);
            
        }

        [TestCleanup]
        public void Shutdown()
        {
            if (_engine == null) return;

            _engine.Dispose();
            _engine = null;
            _database = null;
        }

        [TestMethod]
        public void TestAddBill()
        {
            _database.PurgeAsync().Wait();

            var bill = new Bill
                           {
                Id = Guid.NewGuid(),
                Name = "Test"
            };

            _database.SaveAsync( bill ).Wait();
            
            var person1 = new Person
                              {
                Id = Guid.NewGuid(),
                Name = "Martin"
            };

            _database.SaveAsync( person1 ).Wait();

            var partaker1 = new Partaker
                                {
                Id = Guid.NewGuid(),
                Paid = 42,
                Person = person1
            };

            bill.Partakers.Add(partaker1);

            _database.SaveAsync( bill ).Wait();

            var person2 = new Person
                              {
                Id = Guid.NewGuid(),
                Name = "Jeremy"
            };

            _database.SaveAsync( person2 ).Wait();
            
            var partaker2 = new Partaker
                                {
                Id = Guid.NewGuid(),
                Paid = 0,
                Person = person2
            };

            bill.Partakers.Add(partaker2);

            _database.SaveAsync( bill ).Wait();

            var partaker3 = new Partaker()
                                {
                                    Id = Guid.NewGuid(),
                                    Paid = 1,
                                    Person = person1
                                };

            bill.Partakers.Add(partaker3);

            _database.SaveAsync( bill ).Wait();

            _database.FlushAsync().Wait();
            
            var billKeys = _database.Query<Bill, Guid>();

            Assert.IsTrue(billKeys.Count == 1);
            Assert.AreEqual(billKeys[0].Key, bill.Id);

            Shutdown();
            Init();

            // Check ids
            billKeys = _database.Query<Bill, Guid>();

            Assert.IsTrue(billKeys.Count == 1);
            Assert.AreEqual(billKeys[0].Key, bill.Id);

            var freshBill = billKeys[0].LazyValue.Value;

            Assert.IsTrue(freshBill.Partakers.Count == 3, "Bill should have exactly 3 partakers.");            

            var personKeys = _database.Query<Person, Guid>();

            Assert.IsTrue(personKeys.Count == 2, "Failed to save exactly 2 persons.");            
            
            // Compare loaded instances and verify they are equal 
            var persons = (from p in freshBill.Partakers where p.Person.Id.Equals(person1.Id) select p.Person).ToList();

            // should be two of these
            Assert.AreEqual(2, persons.Count, "Failed to grab two instances of the same person.");
            Assert.AreEqual(persons[0], persons[1], "Instances were not equal.");
        }
    }
}
