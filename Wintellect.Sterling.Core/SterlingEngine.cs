
using System;
using Wintellect.Sterling.Core.Database;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    ///     Wrapper for the sterling database engine
    /// </summary>
    public class SterlingEngine : IDisposable
    {
        private Lazy<SterlingDatabase> _database = null;

        /// <summary>
        ///     The database engine
        /// </summary>
        public ISterlingDatabase SterlingDatabase
        {
            get { return _database.Value; }
        }

        public ISterlingPlatformAdapter PlatformAdapter { get; private set; }

        public void Reset()
        {
            _database = new Lazy<SterlingDatabase>( () => new SterlingDatabase( this ) );
        }

        /// <summary>
        ///     Constructor takes in the database 
        /// </summary>
        public SterlingEngine( ISterlingPlatformAdapter platform )
        {
            this.PlatformAdapter = platform;
            _database = new Lazy<SterlingDatabase>( () => new SterlingDatabase( this ) );
        }

        public void Activate()
        {
            _database.Value.Activate();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _database.Value.Deactivate();
        }
    }
}
