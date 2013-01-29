using System.Collections;
using System.Collections.Generic;

namespace Wintellect.Sterling.Server.FileSystem
{
    public static class PathLock
    {
        private static readonly Dictionary<int, object> _pathLocks = new Dictionary<int,object>();

        public static object GetLock(string path)
        {
            var hash = path.GetHashCode();
            lock (((ICollection)_pathLocks).SyncRoot)
            {
                if (_pathLocks.ContainsKey(hash))
                {
                    return _pathLocks[hash];
                }

                var lockObject = new object();
                _pathLocks.Add(hash, lockObject);

                return lockObject;
            }
        }
    }
}