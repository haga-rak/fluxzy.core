// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.ComponentModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("fluxzy")]
[assembly: InternalsVisibleTo("Fluxzy.Desktop.Services")]
[assembly: InternalsVisibleTo("Fluxzy.Desktop.Ui")]
[assembly: InternalsVisibleTo("Fluxzy.Encoding.Tests")]
[assembly: InternalsVisibleTo("Fluxzy.Bulk.BcCli")]
[assembly: InternalsVisibleTo("Fluxzy.Tests")]
[assembly: InternalsVisibleTo("fluxzynetcap")]
[assembly: InternalsVisibleTo("Fluxzy.Core.Pcap.Cli")]

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit
    {
    }
}
