using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class PackageInfo
	{
		public PackageInfo(string id, string version)
		{
			Id = id;
			Version = NuGetVersion.Parse(version);
		}

		public string Id { get; }
		public NuGetVersion Version { get; }
	}
}