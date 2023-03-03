// // Copyright 2022 - Haga Rakotoharivelo
// 

using System.Reflection;

namespace Fluxzy
{
    public static class FluxzyMetaInformation
    {
        private static string? _version;

        public static string? Version
        {
            get
            {
                if (_version != null)
                    return _version;

                return _version = typeof(Proxy).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                               .InformationalVersion ?? "1.0.0";
            }
        }
    }
}