using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public record CompatibilityInfo
	{
		public CompatibilityInfo(PackageInfo package) => (Id, Version) = package;

		public CompatibilityInfo(IPackageSearchMetadata metadata)
		{
			Id = metadata.Identity.Id;
			Version = metadata.Identity.Version;
		}

		public string Id { get; }
		public NuGetVersion Version { get; }
		public NuGetVersion? EarliestCompatible { get; init; }
		public NuGetVersion? LatestCompatible { get; init; }
		public CompatibilityStatus Status { get; init; }
	}

	public enum CompatibilityStatus
	{
		NotCompatible,
		DependenciesCompatible,
		FullyCompatible,
		NotFound
	}
}