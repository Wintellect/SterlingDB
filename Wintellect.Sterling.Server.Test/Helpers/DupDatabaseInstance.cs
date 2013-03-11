using System.Collections.Generic;
using Wintellect.Sterling.Core;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Test.Helpers
{
    public class DupDatabaseInstance : BaseDatabaseInstance 
    {
        /// <summary>
        ///     Method called from the constructor to register tables
        /// </summary>
        /// <returns>The list of tables for the database</returns>
        protected override List<ITableDefinition> RegisterTables()
        {
            return new List<ITableDefinition>
                       {
                           CreateTableDefinition<TestModel, int>(testModel => testModel.Key),
                           CreateTableDefinition<TestModel, string>(testModel => testModel.Data)
                       };
        }
    }
}
