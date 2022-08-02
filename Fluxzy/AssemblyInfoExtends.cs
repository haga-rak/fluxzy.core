using System.ComponentModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Fluxzy.Cli")]
[assembly: InternalsVisibleTo("Fluxzy.Desktop.Services")]
[assembly: InternalsVisibleTo("Fluxzy.Desktop.Ui")]
[assembly: InternalsVisibleTo("Fluxzy.Encoding.Tests")]
[assembly: InternalsVisibleTo("Fluxzy.Tests")]

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}