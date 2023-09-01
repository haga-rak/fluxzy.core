using System.Runtime.InteropServices;

var genericRuntimeIdentifier = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? "win" : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
        "linux" : "osx";

genericRuntimeIdentifier += $"-{(RuntimeInformation.OSArchitecture).ToString().ToLower()}";

var version = Console.In.ReadToEnd().Trim('\r', '\n', ' ', '\t');
var fileName = $"fxzy-{version}-{genericRuntimeIdentifier}.zip"; 

var fullFileName = Path.Combine(Args[0], fileName);

if (System.IO.File.Exists(fullFileName)) {
    System.IO.File.Delete(fullFileName);
}

System.IO.Compression.ZipFile.CreateFromDirectory(
          Args[1],
          fullFileName,
          System.IO.Compression.CompressionLevel.Optimal,
          false
       );
Console.Write($"{fileName}");