using CsvHelper.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester.Model
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

	public class CompatibilityReportMap : ClassMap<CompatibilityInfo>
	{
		public CompatibilityReportMap()
		{
			int i = 0;
			Map(m => m.Id).Index(i++).Name("id");
			Map(m => m.Version).Index(i++).Name("version");
			Map(m => m.EarliestCompatible).Index(i).Name("earliest_compatible");
			Map(m => m.LatestCompatible).Index(i).Name("latest_compatible");
			Map(m => m.Status).Index(i).Name("status");
		}
	}
}