using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class CompatibilityInfo
	{
		public CompatibilityInfo(PackageInfo package)
		{
			Id = package.Id;
			Version = package.Version;
		}

		public CompatibilityInfo(IPackageSearchMetadata metadata)
		{
			Id = metadata.Identity.Id;
			Version = metadata.Identity.Version;
		}

		public string Id { get; }
		public NuGetVersion Version { get; }
		public NuGetVersion? EarliestCompatible { get; set; }
		public CompatibilityStatus Status { get; set; }
	}

	public enum CompatibilityStatus
	{
		NotCompatible,
		DependenciesCompatible,
		FullyCompatible,
		NotFound
	}
}