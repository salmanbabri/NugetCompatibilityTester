using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NugetCompatibilityTester.Extensions;
using NugetCompatibilityTester.Model;
using NugetCompatibilityTester.Services;

namespace NugetCompatibilityTester.BusinessLogic
{
	public class CompatibilityAnalyzer
	{
		private readonly CompatibilityService _compatibilityService;
		private readonly CompatibilityConfig _config;

		public CompatibilityAnalyzer(CompatibilityService compatibilityService, CompatibilityConfig config)
		{
			_compatibilityService = compatibilityService;
			_config = config;
		}

		public bool SupportsModernPlatform(IPackageSearchMetadata packageMetadata)
		{
			var modernFrameworks = new List<string> { ".NETStandard", ".NETCoreApp" };
			return packageMetadata.DependencySets.Any(g => modernFrameworks.Contains(g.TargetFramework.Framework));
		}


		public async Task<CompatibilityStatus> GetStatus(IPackageSearchMetadata packageMetadata)
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
			bool hasCompatibleVersion = _config.Version is null ||
			                            version.Major == _config.Version.Major && version.Minor == _config.Version.Minor;

			return group.TargetFramework.Framework.Equals(_config.Framework) && hasCompatibleVersion;
		}

		//Needed in cases where main package is mainly based on it's dependencies. Eg: Humanizer
		//Can be unreliable, as .NET Framework ONLY package could internally use new .NET Standard compliant dependencies. Example: Autofac.WebApi2
		private async Task<CompatibilityStatus> AnalyzeDependency(PackageDependency dependency)
		{
			var allMetaData = await _compatibilityService.GetAllPackageMetadata(dependency.Id);
			var metaData = allMetaData.FindClosestVersion(dependency.VersionRange.MinVersion);

			return metaData is null
				? CompatibilityStatus.NotCompatible
				: await GetStatus(metaData);
		}
	}
}