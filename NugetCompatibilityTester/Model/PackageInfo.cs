using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class PackageInfo
	{
		public string Id { get; set; }
		public NuGetVersion Version { get; set; }
	}
}