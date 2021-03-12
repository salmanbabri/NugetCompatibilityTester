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

		public string Id { get; init; }
		public NuGetVersion Version { get; init; }
		public NuGetVersion? EarliestCompatible { get; set; }
		public CompatibilityStatus Status { get; set; }
	}

	public enum CompatibilityStatus
	{
		NotCompatible,
		Undecided,
		Compatible,
		NotFound
	}
}