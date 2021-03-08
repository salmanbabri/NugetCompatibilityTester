using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	class Program
	{
		static async Task Main()
		{
			var host = GetHostBuilder().Build();

			using var serviceScope = host.Services.CreateScope();
			var services = serviceScope.ServiceProvider;

			var sdkService = services.GetRequiredService<NugetSdkCompatibility>();

			var xml = XDocument.Load("demo.config");

			var packages = xml.Descendants("package")
			                  .Select(c => new PackageInfo
			                  {
				                  Id = c.Attribute("id")!.Value,
				                  Version = NuGetVersion.Parse(c.Attribute("version")!.Value)
			                  })
			                  .ToArray();

			await sdkService.CheckCompatibility(packages);
		}

		private static IHostBuilder GetHostBuilder()
		{
			return new HostBuilder()
			       .ConfigureServices((_, services) =>
			       {
				       services.AddHttpClient();
				       services.AddHttpClient("decompress_gzip").ConfigurePrimaryHttpMessageHandler(_ =>
				       {
					       var handler = new HttpClientHandler();

					       if (handler.SupportsAutomaticDecompression)
						       handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

					       return handler;
				       });
				       services.AddTransient<NugetApiSearch>();
				       services.AddTransient<NugetSdkCompatibility>();
			       })
			       .UseConsoleLifetime();
		}
	}
}

/*
	 services.AddHttpClient<XApiClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
	{
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
	}).AddPolicyHandler(request => timeout);
*/