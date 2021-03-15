using NuGet.Versioning;

namespace NugetCompatibilityTester.Model
{
	public record PackageInfo(string Id, NuGetVersion Version);
}