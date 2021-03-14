using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public record PackageInfo(string Id, NuGetVersion Version);
}