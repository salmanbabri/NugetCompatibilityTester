using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class NugetSdkCompatibility
	{
		private readonly CompatibilityService _compatibilityService;
		public CompatibilityConfig Config { get; } = new();

		public NugetSdkCompatibility(CompatibilityService compatibilityService)
		{
			_compatibilityService = compatibilityService;
		}

		public Task<CompatibilityInfo[]> GetCompatibilityReportAsync(CompatibilityInput input)
		{
			var result = input.Packages.Select(async p =>
			{
				var compatibilityInfo = await GetCompatibilityInfo(p);
				input.Updates?.Report(p.Id);
				return compatibilityInfo;
			});

			return Task.WhenAll(result);
		}

		private async Task<CompatibilityInfo> GetCompatibilityInfo(PackageInfo package)
		{
			var allMetaData = await _compatibilityService.GetAllPackageMetadata(package.Id);
			var packageMetaData = allMetaData.FindClosestVersion(package.Version);

			if (packageMetaData is null)
				return new CompatibilityInfo(package) { Status = CompatibilityStatus.NotFound };

			var status = await GetCompatibilityStatus(packageMetaData);

			var filteredMetadata = status is not CompatibilityStatus.FullyCompatible
				? allMetaData.Where(p => p.Identity.Version > package.Version)
				: allMetaData;

			return new CompatibilityInfo(package)
			{
				Status = status,
				EarliestCompatible = await FindEarliestCompatibleVersion(filteredMetadata)
			};
		}

		private async Task<CompatibilityStatus> GetCompatibilityStatus(IPackageSearchMetadata packageMetadata)
		{
			var dependencyGroups = packageMetadata.DependencySets.ToList();

			if (dependencyGroups.Count is 0)
				return CompatibilityStatus.NotCompatible;

			if (dependencyGroups.Any(HasConfigFramework))
				return CompatibilityStatus.FullyCompatible;

			var areAllDependenciesCompatible = await dependencyGroups
			                                         .Where(d => d.TargetFramework == NuGetFramework.AnyFramework)
			                                         .SelectMany(d => d.Packages)
			                                         .ToAsyncEnumerable()
			                                         .SelectAwait(async p => await AnalyzeDependency(p))
			                                         .AllAsync(s => s == CompatibilityStatus.FullyCompatible);

			return areAllDependenciesCompatible ? CompatibilityStatus.DependenciesCompatible : CompatibilityStatus.NotCompatible;
		}

		private bool HasConfigFramework(IFrameworkSpecific group)
		{
			var version = group.TargetFramework.Version;
			bool hasCompatibleVersion = Config.Version is null ||
			                            version.Major == Config.Version.Major && version.Minor == Config.Version.Minor;

			return group.TargetFramework.Framework.Equals(Config.Framework) && hasCompatibleVersion;
		}

		//Needed in cases where main package is mainly based on it's dependencies. Eg: Humanizer
		//Can be unreliable, as .NET Framework ONLY package could internally use new .NET Standard compliant dependencies. Example: Autofac.WebApi2
		private async Task<CompatibilityStatus> AnalyzeDependency(PackageDependency dependency)
		{
			var allMetaData = await _compatibilityService.GetAllPackageMetadata(dependency.Id);
			var metaData = allMetaData.FindClosestVersion(dependency.VersionRange.MinVersion);

			return metaData is null
				? CompatibilityStatus.NotCompatible
				: await GetCompatibilityStatus(metaData);
		}

		private async Task<NuGetVersion?> FindEarliestCompatibleVersion(IEnumerable<IPackageSearchMetadata> packageMetadata)
		{
			return await packageMetadata.ToAsyncEnumerable()
			                            .SelectAwait(async p => new CompatibilityInfo(p) { Status = await GetCompatibilityStatus(p) })
			                            .SkipWhile(c => c.Status is not CompatibilityStatus.FullyCompatible)
			                            .Select(c => c.Version)
			                            .FirstOrDefaultAsync();
		}
	}
}