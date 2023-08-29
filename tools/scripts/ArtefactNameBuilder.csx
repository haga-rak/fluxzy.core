using System.Runtime.InteropServices;

var version = Console.In.ReadToEnd().Trim('\r', '\n', ' ', '\t');
Console.Write($"fxzy-{version}-{RuntimeInformation.RuntimeIdentifier}.zip");

