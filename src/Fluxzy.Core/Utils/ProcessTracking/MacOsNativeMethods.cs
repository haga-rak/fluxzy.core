// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Runtime.InteropServices;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// P/Invoke declarations for macOS process tracking APIs.
    /// </summary>
    internal static class MacOsNativeMethods
    {
        public const int PROC_ALL_PIDS = 1;
        public const int PROC_PIDLISTFDS = 1;
        public const int PROC_PIDFDSOCKETINFO = 3;
        public const uint PROX_FDTYPE_SOCKET = 2;
        public const int PROC_FDINFO_SIZE = 8; // sizeof(proc_fdinfo)
        public const int SOCKET_FDINFO_SIZE = 808; // sizeof(socket_fdinfo)
        public const int PROC_PIDPATHINFO_MAXSIZE = 4096;

        public const int CTL_KERN = 1;
        public const int KERN_PROCARGS2 = 49;

        [DllImport("libproc.dylib")]
        public static extern int proc_listpids(int type, uint typeinfo, IntPtr buffer, int buffersize);

        [DllImport("libproc.dylib")]
        public static extern int proc_pidinfo(int pid, int flavor, ulong arg, IntPtr buffer, int buffersize);

        [DllImport("libproc.dylib")]
        public static extern int proc_pidfdinfo(int pid, int fd, int flavor, IntPtr buffer, int buffersize);

        [DllImport("libproc.dylib")]
        public static extern int proc_pidpath(int pid, IntPtr buffer, uint buffersize);

        [DllImport("libc.dylib")]
        public static extern int sysctl(int[] name, int namelen, IntPtr oldp, ref IntPtr oldlenp, IntPtr newp, IntPtr newlen);
    }
}
