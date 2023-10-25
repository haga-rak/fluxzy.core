// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Fluxzy.Utils
{
    // On linux and OSX, .NET returns /tmp/ directory which requires elevation to write to.
    // Instead we use %appdata%/.fluxzy/temp directory with %appdata% a custom environment variable
    // specific to fluxzy
    internal static class ExtendedPathHelper
    {
        public static string GetTempPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Path.GetTempPath();
            
            
            var fullPath = Environment.ExpandEnvironmentVariables("%appdata%/.fluxzy/temp");
            Directory.CreateDirectory(fullPath);

            return fullPath; 
        }

        public static string GetTempFileName()
        {
            var tempPath = GetTempPath(); 
            
            var fileName = Path.GetRandomFileName();
            
            return Path.Combine(tempPath, fileName);
        }
    }
}
