using System.Net;
using Fluxzy;

namespace Samples.No001.SimpleCapture
{
	internal class Program
	{
		/// <summary>
		/// In this sample, we will create a simple proxy that will capture all the traffic from
		/// an HttpClient and save it to an fxzy file or HAR file 
		/// </summary>
		static async Task Main()
		{
			var tempDirectory = "capture_dump";

			// Create a default run settings 
			var fluxzyStartupSetting = FluxzySetting
			                           .CreateDefault(IPAddress.Loopback, 44344)
			                           .AddBoundAddress(IPAddress.IPv6Loopback, 44344) // add extra binding address
			                           .SetOutDirectory(tempDirectory)
			                           .SetAutoInstallCertificate(true) // Fluxzy will install the certificate to the default machine store
			                           ;
			
			// Create a proxy instance
			await using (var proxy = new Proxy(fluxzyStartupSetting))
			{
				var endpoints = proxy.Run();

				using var httpClient = new HttpClient(new HttpClientHandler()
		{

				await (await response.Content.ReadAsStreamAsync()).CopyToAsync(Stream.Null); 
			}

			// Packing the output files must be after the proxy dispose because some files may 
			// remain write-locked. 

			// Pack the files into fxzy file. This is the recommended file format as it can holds raw capture datas. 
			Packager.Export(tempDirectory, "mycapture.fxzy");

		}
	}
}