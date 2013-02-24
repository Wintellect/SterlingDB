
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wintellect.Sterling.Core;

namespace Wintellect.Sterling.Test
{
    public abstract class TestBase
    {
        protected virtual ISterlingDriver GetDriver( string test )
        {
            return new MemoryDriver();
        }
    }
}
