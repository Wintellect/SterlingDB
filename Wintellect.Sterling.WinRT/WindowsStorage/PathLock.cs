using System.Collections;
using System.Collections.Generic;
using Wintellect.Sterling.Core;

namespace Wintellect.Sterling.WinRT.WindowsStorage
{
    internal static class PathLock
    {
        private static readonly Dictionary<int, AsyncLock> _pathLocks = new Dictionary<int,AsyncLock>();

        public static AsyncLock GetLock(string path)
        {
            var hash = path.GetHashCode();

            lock (((ICollection)_pathLocks).SyncRoot)
            {
                AsyncLock alock = null;

                if (_pathLocks.TryGetValue( hash, out alock ) == false )
                {
                    alock = _pathLocks[ hash ] = new AsyncLock();
                }

                return alock;
            }
        }
    }
}