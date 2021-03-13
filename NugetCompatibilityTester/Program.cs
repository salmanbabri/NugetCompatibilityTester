using System;
using System.Diagnostics;
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

			var xml = XDocument.Load("demo.config");
			var packages = xml.Descendants("package")
			                  .Select(c => new PackageInfo
			                  {
				                  Id = c.Attribute("id")!.Value,
				                  Version = NuGetVersion.Parse(c.Attribute("version")!.Value)
			                  })
			                  .ToList();

			var sdkService = services.GetRequiredService<NugetSdkCompatibility>();
			sdkService.Config.Framework = ".NETStandard";
			sdkService.Config.Version = new Version(2, 0);

			var timer = new Stopwatch();
			timer.Start();

			var progress = new Progress<string>();
			int count = 0;
			progress.ProgressChanged += (_, package)
				=> Console.WriteLine($"Package processed: {package}, Total completed: {++count}/{packages.Count}, Time: {timer.Elapsed:m\\:ss\\.fff}");

			var input = new CompatibilityInput { Packages = packages, Updates = progress };

			var report = await sdkService.GetCompatibilityReportAsync(input);

			foreach (var p in report)
				Console.WriteLine($"package: {p.Id}, version: {p.Version}, status: {p.Status}, earliest: {p.EarliestCompatible}, latest: {p.LatestCompatible}");

			timer.Stop();
			Console.WriteLine($"Total time taken: {timer.Elapsed:m\\:ss\\.fff}");
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
				       services.AddSingleton<CompatibilityService>();
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