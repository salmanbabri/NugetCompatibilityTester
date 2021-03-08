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

			// var apiService = services.GetRequiredService<NugetApiSearch>();
			// await apiService.Search("Newtonsoft.Json", "12.0.3");
			// await apiService.Search("Newtonsoft.Json", "9.0.1");

			var sdkService = services.GetRequiredService<NugetSdkSearch>();
			// await sdkService.Search("Newtonsoft.Json", "12.0.3");
			// await sdkService.Search("Humanizer.Core", "2.8.26");
			// await sdkService.Search("Humanizer.Core.uk", "2.8.26");
			await sdkService.Search("Humanizer", "2.8.26");
			// await sdkService.Search("Newtonsoft.Json", "9.0.1");
			// await myService.Search("xunit", "2.4.1");
			// await myService.Search("Humanizer.Core", "2.8.26");
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
				       services.AddTransient<NugetSdkSearch>();
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