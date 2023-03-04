// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Text.RegularExpressions;

namespace Fluxzy.NativeOps.SystemProxySetup.macOs
{
    internal class Interface
    {
        public Interface(int index, string name, string deviceName)
        {
            Index = index;
            Name = name;
            DeviceName = deviceName;
        }

        public string Name { get;  } 

        public string DeviceName { get;  } 

        public int Index { get;  }
        
        public bool Up { get; set; }

        public static Interface?  BuildFrom(string[] lines)
        {
            if (lines.Length != 2)
                return null;

            var regexInterfaceName = @"^\((\d+)\) (.*)$"; 
            var regexDeviceName = @"Device: ([a-zA-Z0-9_]+)\)$";

            var matchInterfaceName = Regex.Match(lines[0], regexInterfaceName);

            if (!matchInterfaceName.Success)
                return null;

            var interfaceName = matchInterfaceName.Groups[2].Value; 
            var deviceIndex = int.Parse(matchInterfaceName.Groups[1].Value);

            var matchDeviceName = Regex.Match(lines[1], regexDeviceName);

            if (!matchDeviceName.Success) 
                return null;

            var deviceName = matchDeviceName.Groups[1].Value;

            return new Interface(deviceIndex, interfaceName, deviceName);
        }
    }
}
