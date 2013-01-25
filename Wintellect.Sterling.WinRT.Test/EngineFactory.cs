
using System;

using Wintellect.Sterling.Core;

namespace Wintellect.Sterling.Test
{
    internal static class Factory
    {
        public static SterlingEngine NewEngine()
        {
            return new SterlingEngine( NewPlatformAdapter() );
        }

        public static ISterlingPlatformAdapter NewPlatformAdapter()
        {
            return new Wintellect.Sterling.WinRT.PlatformAdapter();
        }
    }
}
