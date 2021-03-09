using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class CompatibilityInfo
	{
		public string Id { get; set; }
		public NuGetVersion Version { get; set; }
		public bool IsCompatible { get; set; }
	}
}