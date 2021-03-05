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
			var searchResponse = await GetFromSearchApiAsync(packageId);
			var versionInUse = GetVersionInformation(searchResponse).First(v => v.Version == version);

			var registrationLeaf = await GetRegistrationLeafAsync(versionInUse.DetailUrl);
			string catalogUrl = GetCatalogUrl(registrationLeaf);

			var catalogLeaf = await GetCatalogLeafAsync(catalogUrl);

			var targetFrameworks = GetTargetFrameworks(catalogLeaf);
			Console.WriteLine($"Package {packageId}, {version}: Supported frameworks: {string.Join(", ", targetFrameworks)}");
		}

		private async Task<JObject> GetFromSearchApiAsync(string packageId)
		{
			string url = $"https://azuresearch-usnc.nuget.org/query?q=PackageId:{packageId}&semVerLevel=2.0.0";
			var request = new HttpRequestMessage(HttpMethod.Get, url);
			using var client = _factory.CreateClient();
			var response = await client.SendAsync(request);
			var content = await response.Content.ReadAsStringAsync();

			return JObject.Parse(content);
		}

		private IEnumerable<NugetVersion> GetVersionInformation(JObject searchResponse)
		{
			var versionInformation = searchResponse["data"]![0]!["versions"]!.ToList();

			return versionInformation.Select(v => new NugetVersion
			{
				Version = v["version"]!.ToString(),
				DetailUrl = v["@id"]!.ToString()
			});
		}

		private async Task<JObject> GetRegistrationLeafAsync(string leafUrl)
		{
			using var client = _factory.CreateClient("decompress_gzip");
			var request = new HttpRequestMessage(HttpMethod.Get, leafUrl);
			var response = await client.SendAsync(request);

			var content = await response.Content.ReadAsStringAsync();

			return JObject.Parse(content);
		}

		private static string GetCatalogUrl(JObject registrationLeaf)
			=> registrationLeaf["catalogEntry"]!.ToString();

		private async Task<JObject> GetCatalogLeafAsync(string catalogUrl)
		{
			using var client = _factory.CreateClient("decompress_gzip");
			var request = new HttpRequestMessage(HttpMethod.Get, catalogUrl);
			var response = await client.SendAsync(request);

			var content = await response.Content.ReadAsStringAsync();

			return JObject.Parse(content);
		}

		private List<string> GetTargetFrameworks(JObject catalogLeaf)
		{
			var dependencyGroups = catalogLeaf["dependencyGroups"]?.ToList() ?? new List<JToken>();
			return dependencyGroups.Select(d => d["targetFramework"]!.ToString()).ToList();
		}
	}

	public class NugetVersion
	{
		public string Version { get; set; } = "";
		public string DetailUrl { get; set; } = "";
	}
}