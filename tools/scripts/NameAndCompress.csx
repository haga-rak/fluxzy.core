using System.Runtime.InteropServices;

var version = Console.In.ReadToEnd().Trim('\r', '\n', ' ', '\t');

var shortIdentifier = Operating​System.IsWindows() ?
    "windows" : (OperatingSystem.IsMacOS() ? "macos" :
        (OperatingSystem.IsLinux() ? "linux" : "custom"));

shortIdentifier += $"-{RuntimeInformation.ProcessArchitecture}";
shortIdentifier = shortIdentifier.ToLowerInvariant();

var fileName = $"fluxzy-{version}-{shortIdentifier}.zip"; 

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