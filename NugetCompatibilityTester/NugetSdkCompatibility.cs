using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
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

		public async IAsyncEnumerable<CompatibilityInfo> GetCompatibilityReport(CompatibilityInput input)
		{
			foreach (var package in input.Packages)
			{
				var allMetaData = await _compatibilityService.GetAllPackageMetadata(package.Id);
				var packageMetaData = allMetaData.FindClosestVersion(package.Version);

				yield return packageMetaData is null
					? PackageNotFound()
					: await PackageFound();

				CompatibilityInfo PackageNotFound() => new(package) { Status = CompatibilityStatus.NotFound };

				async Task<CompatibilityInfo> PackageFound() => new(package)
				{
					EarliestCompatible = await FindEarliestCompatibleVersion(allMetaData),
					Status = await GetCompatibilityStatus(packageMetaData)
				};
			}
		}

		private async Task<CompatibilityStatus> GetCompatibilityStatus(IPackageSearchMetadata packageMetadata)
		{
			var dependencyGroups = packageMetadata.DependencySets.ToList();

			if (dependencyGroups.Count is 0)
				return CompatibilityStatus.NotCompatible;

			if (dependencyGroups.Any(HasConfigFramework))
				return CompatibilityStatus.FullyCompatible;

			var areDependenciesCompatible = await AnalyzeDependencies(dependencyGroups).AllAsync(d => d == CompatibilityStatus.FullyCompatible);

			return areDependenciesCompatible ? CompatibilityStatus.DependenciesCompatible : CompatibilityStatus.NotCompatible;
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
		private async IAsyncEnumerable<CompatibilityStatus> AnalyzeDependencies(IEnumerable<PackageDependencyGroup> dependencyGroups)
		{
			var dependencies = dependencyGroups.Where(d => d.TargetFramework == NuGetFramework.AnyFramework)
			                                   .SelectMany(d => d.Packages)
			                                   .ToList();

			foreach (var package in dependencies)
			{
				var allMetaData = await _compatibilityService.GetAllPackageMetadata(package.Id);
				var metaData = allMetaData.FindClosestVersion(package.VersionRange.MinVersion);

				yield return metaData is null
					? CompatibilityStatus.NotCompatible
					: await GetCompatibilityStatus(metaData);
			}
		}

		private async Task<NuGetVersion?> FindEarliestCompatibleVersion(IEnumerable<IPackageSearchMetadata> packageMetadata)
		{
			return await GetPackageCompatibility()
			             .SkipWhile(c => c.Status is not CompatibilityStatus.FullyCompatible)
			             .Select(c => c.Version)
			             .FirstOrDefaultAsync();

			async IAsyncEnumerable<CompatibilityInfo> GetPackageCompatibility()
			{
				foreach (var metadata in packageMetadata)
					yield return new CompatibilityInfo(metadata) { Status = await GetCompatibilityStatus(metadata) };
			}
		}
	}
}