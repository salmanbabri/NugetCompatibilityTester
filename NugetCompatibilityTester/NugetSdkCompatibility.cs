using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugetCompatibilityTester
{
	public class NugetSdkCompatibility
	{
		public async Task CheckCompatibility(params PackageInfo[] packages)
		{
			foreach (var package in packages)
			{
				var allMetaData = (await GetAllPackageMetadata(package.Id)).ToList();
				var packageMetaData = allMetaData.First(p => p.Identity.Version.Equals(package.Version));

				bool hasDotNetStandardSupport = await IsCompatible(packageMetaData);
				Console.WriteLine($"package: {package.Id}, version: {package.Version}, compatibility: {hasDotNetStandardSupport}");

				var earliestCompatible = await FindEarliestSupportingVersion(allMetaData);

				Console.WriteLine(earliestCompatible is null
					? "Compatible version not found"
					: $"Earliest compatible version: {earliestCompatible.Version}");
			}
		}

		private async Task<CompatibilityInfo?> FindEarliestSupportingVersion(IEnumerable<IPackageSearchMetadata> packageMetadata)
		{
			return await GetCompatibilityReport(packageMetadata).SkipWhile(c => !c.IsCompatible).FirstOrDefaultAsync();
		}

		private async IAsyncEnumerable<CompatibilityInfo> GetCompatibilityReport(IEnumerable<IPackageSearchMetadata> packageMetadata)
		{
			foreach (var metadata in packageMetadata)
			{
				yield return new CompatibilityInfo
				{
					Id = metadata.Identity.Id,
					Version = metadata.Identity.Version,
					IsCompatible = await IsCompatible(metadata)
				};
			}
		}

		private async Task<bool> IsCompatible(IPackageSearchMetadata packageMetadata)
		{
			var dependencyGroups = packageMetadata.DependencySets.ToList();

			if (dependencyGroups.Count is 0)
				return false;

			if (dependencyGroups.Any(d => d.TargetFramework.Framework.Equals(".NETStandard")))
				return true;

			var isAnyDependencyCompatible = await AnalyzeDependencies(dependencyGroups).FirstOrDefaultAsync(d => d);

			return isAnyDependencyCompatible;
		}

		private async IAsyncEnumerable<bool> AnalyzeDependencies(IEnumerable<PackageDependencyGroup> dependencyGroups)
		{
			var dependencies = dependencyGroups.Where(d => d.TargetFramework == NuGetFramework.AnyFramework)
			                                   .SelectMany(d => d.Packages)
			                                   .ToList();

			foreach (var package in dependencies)
			{
				var metaData = (await GetAllPackageMetadata(package.Id)).First(p => p.Identity.Version.Equals(package.VersionRange.MinVersion));
				yield return await IsCompatible(metaData);
			}
		}

		private async Task<IEnumerable<IPackageSearchMetadata>> GetAllPackageMetadata(string packageId)
		{
			ILogger logger = NullLogger.Instance;
			var cancellationToken = CancellationToken.None;
			var cache = new SourceCacheContext();

			var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

			return await resource.GetMetadataAsync(
				packageId,
				includePrerelease: false,
				includeUnlisted: false,
				cache,
				logger,
				cancellationToken);
		}
	}
}