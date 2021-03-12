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

		public IAsyncEnumerable<CompatibilityInfo> GetCompatibilityReport(CompatibilityInput input)
		{
			return input.Packages.ToAsyncEnumerable().SelectAwait(async p => await GetCompatibilityInfo(p));
		}

		private async Task<CompatibilityInfo> GetCompatibilityInfo(PackageInfo package)
		{
			var allMetaData = await _compatibilityService.GetAllPackageMetadata(package.Id);
			var packageMetaData = allMetaData.FindClosestVersion(package.Version);

			return packageMetaData is null
				? new CompatibilityInfo(package) { Status = CompatibilityStatus.NotFound }
				: new CompatibilityInfo(package)
				{
					EarliestCompatible = await FindEarliestCompatibleVersion(allMetaData),
					Status = await GetCompatibilityStatus(packageMetaData)
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