using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			var timer = new Stopwatch();
			timer.Start();

			foreach (var package in packages)
			{
				var allMetaData = (await GetAllPackageMetadata(package.Id)).ToList();
				var packageMetaData = allMetaData.FindClosestVersion(package.Version);

				if (packageMetaData is null)
				{
					Console.WriteLine($"Package {package.Id} with version {package.Version} not found on public nuget repository.");
					continue;
				}

				bool hasDotNetStandardSupport = await IsCompatible(packageMetaData);
				Console.WriteLine($"package: {package.Id}, version: {package.Version}, compatibility: {hasDotNetStandardSupport}");

				var earliestCompatible = await FindEarliestSupportingVersion(allMetaData);

				Console.WriteLine(earliestCompatible is null
					? "Compatible version not found"
					: $"Earliest compatible version: {earliestCompatible.Version}");
			}

			timer.Stop();
			Console.WriteLine($"Total time: {timer.Elapsed:m\\:ss\\.fff}");
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

			var isAnyDependencyCompatible = await AnalyzeDependencies(dependencyGroups).AllAsync(d => d);

			return isAnyDependencyCompatible;
		}

		//Needed in cases where main package is mainly based on it's dependencies. Eg: Humanizer
		//Can be unreliable, as .NET Framework ONLY package could internally use new .NET Standard compliant dependencies. Example: Autofac.WebApi2
		private async IAsyncEnumerable<bool> AnalyzeDependencies(IEnumerable<PackageDependencyGroup> dependencyGroups)
		{
			var dependencies = dependencyGroups.Where(d => d.TargetFramework == NuGetFramework.AnyFramework)
			                                   .SelectMany(d => d.Packages)
			                                   .ToList();

			foreach (var package in dependencies)
			{
				var allMetaData = (await GetAllPackageMetadata(package.Id)).ToList();
				var metaData = allMetaData.FindClosestVersion(package.VersionRange.MinVersion);

				yield return metaData is not null && await IsCompatible(metaData);
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