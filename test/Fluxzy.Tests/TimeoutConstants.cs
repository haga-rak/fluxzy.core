// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Runtime.InteropServices;

namespace Fluxzy.Tests
{
    public static class TimeoutConstants
    {
        public static int Regular {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    return 40;
                }
                else {
                    return 15;
                }
            }
        }
        public static int Extended {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    return 60;
                }
                else {
                    return 30;
                }
            }
        }
    }
}
