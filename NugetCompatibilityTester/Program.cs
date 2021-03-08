using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NugetCompatibilityTester
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var host = GetHostBuilder().Build();

			using var serviceScope = host.Services.CreateScope();
			var services = serviceScope.ServiceProvider;

			var sdkService = services.GetRequiredService<NugetSdkCompatibility>();

			await sdkService.CheckCompatibility(
				new[]
				{
					new PackageInfo("Newtonsoft.Json", "12.0.3"),
					new PackageInfo("Humanizer.Core", "2.8.26"),
					new PackageInfo("Humanizer.Core.uk", "2.8.26"),
					new PackageInfo("Humanizer", "2.8.26"),
					new PackageInfo("Humanizer", "0.1.0"),
					new PackageInfo("Newtonsoft.Json", "9.0.1")
				}
			);
		}

		private static IHostBuilder GetHostBuilder()
		{
			return new HostBuilder()
			       .ConfigureServices((_, services) =>
			       {
				       services.AddHttpClient();
				       services.AddHttpClient("decompress_gzip").ConfigurePrimaryHttpMessageHandler(messageHandler =>
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