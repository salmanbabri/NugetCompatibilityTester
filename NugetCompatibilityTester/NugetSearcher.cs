using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NugetCompatibilityTester
{
	public class NugetSearcher
	{
		private readonly IHttpClientFactory _factory;

		public NugetSearcher(IHttpClientFactory factory)
		{
			_factory = factory;
		}

		public async Task Search(string packageId, string version)
		{
			string url = "https://azuresearch-usnc.nuget.org/query?q=PackageId:Newtonsoft.Json&semVerLevel=2.0.0";
			var request = new HttpRequestMessage(HttpMethod.Get, url);

			var client = _factory.CreateClient();
			var response = await client.SendAsync(request);
			var content = await response.Content.ReadAsStringAsync();

			var foo = JObject.Parse(content);

			var versionInformation = foo["data"]![0]!["versions"]!.ToList();

			var nugetVersions = versionInformation.Select(v => new NugetVersion
			{
				Version = v["version"]!.ToString(),
				DetailUrl = v["@id"]!.ToString()
			}).ToList();

			var versionInUse = nugetVersions.First(v => v.Version == version);

			await ProcessVersionInUse(versionInUse);

			Console.WriteLine("Done");
		}

		private async Task ProcessVersionInUse(NugetVersion versionInUse)
		{
			var client = _factory.CreateClient("decompress_gzip");
			var request = new HttpRequestMessage(HttpMethod.Get, versionInUse.DetailUrl);
			var response = await client.SendAsync(request);

			var content = await response.Content.ReadAsStringAsync();

			var bar = JObject.Parse(content);

			string catalogUrl = bar["catalogEntry"]!.ToString();

			await ProcessCatalog(catalogUrl);

			Console.WriteLine("Done 2");
		}

		private async Task ProcessCatalog(string catalogUrl)
		{
			var client = _factory.CreateClient("decompress_gzip");
			var request = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
			var response = await client.SendAsync(request);

			var content = await response.Content.ReadAsStringAsync();

			var zoo = JObject.Parse(content);

			var dependencies = zoo["dependencyGroups"]?.ToList() ?? new List<JToken>();

			//Todo: Extract .NET frameworks info from dependencies.

			Console.WriteLine("Done 3");
		}
	}

	public class NugetVersion
	{
		public string Version { get; set; } = "";
		public string DetailUrl { get; set; } = "";
	}
}