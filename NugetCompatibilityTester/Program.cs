using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CsvHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGet.Versioning;
using NugetCompatibilityTester.BusinessLogic;
using NugetCompatibilityTester.Model;
using NugetCompatibilityTester.Services;

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
			                  .Select(c => new PackageInfo(c.Attribute("id")!.Value,
				                  NuGetVersion.Parse(c.Attribute("version")!.Value))
			                  )
			                  .ToList();

			var sdkService = services.GetRequiredService<NugetSdkCompatibility>();

			var timer = new Stopwatch();
			timer.Start();

			var progress = new Progress<string>();
			int count = 0;
			progress.ProgressChanged += (_, package)
				=> Console.WriteLine($"Package processed: {package}, Total completed: {++count}/{packages.Count}, Time: {timer.Elapsed:m\\:ss\\.fff}");

			var input = new CompatibilityInput(packages) { Updates = progress };

			var report = await sdkService.GetCompatibilityReportAsync(input);

			timer.Stop();
			Console.WriteLine($"Total time taken: {timer.Elapsed:m\\:ss\\.fff}");

			Console.WriteLine($"Writing to csv");
			await WriteToCsvAsync(report);
			Console.WriteLine($"Successfully written to csv");
		}

		private static async Task WriteToCsvAsync(IEnumerable<CompatibilityInfo> report)
		{
			await using var writer = new StreamWriter("report.csv");
			await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			csv.Context.RegisterClassMap<CompatibilityReportMap>();

			await csv.WriteRecordsAsync(report);
		}

		private static IHostBuilder GetHostBuilder()
		{
			return new HostBuilder()
			       .ConfigureServices((_, services) =>
			       {
				       services.AddTransient<NugetSdkCompatibility>();
				       services.AddSingleton<CompatibilityService>();

				       services.AddTransient<CompatibilityAnalyzer>();
				       services.AddTransient(_ => new CompatibilityConfig(".NETStandard")
				       {
					       Version = new Version(2, 0)
				       });
			       })
			       .UseConsoleLifetime();
		}
	}
}