using System.Runtime.InteropServices;

var version = Console.In.ReadToEnd().Trim('\r', '\n', ' ', '\t');
var fileName = $"fluxzy-{version}-{RuntimeInformation.RuntimeIdentifier}.zip"; 

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