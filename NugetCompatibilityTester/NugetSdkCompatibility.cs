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
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class NugetSdkCompatibility
	{
		public async Task CheckCompatibility(IEnumerable<PackageInfo> packages)
		{
			foreach (var package in packages)
			{
				bool hasDotNetStandardSupport = await IsCompatible(package.Id, package.Version);
				Console.WriteLine($"package: {package.Id}, version: {package.Version}, compatibility: {hasDotNetStandardSupport}");
			}
		}

		private async Task<bool> IsCompatible(string packageId, NuGetVersion version)
		{
			var packageMetadata = await GetPackageMetadata(packageId, version);
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
				yield return await IsCompatible(package.Id, package.VersionRange.MinVersion);
		}

		private async Task<IPackageSearchMetadata> GetPackageMetadata(string packageId, NuGetVersion version)
		{
			ILogger logger = NullLogger.Instance;
			var cancellationToken = CancellationToken.None;
			var cache = new SourceCacheContext();

			var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

			var allPackages = await resource.GetMetadataAsync(
				packageId,
				includePrerelease: false,
				includeUnlisted: false,
				cache,
				logger,
				cancellationToken);

			var package = allPackages.FirstOrDefault(p => p.Identity.Version.Equals(version));

			return package ??
			       throw new ArgumentException(
				       $"No information available for package: {packageId}, version: {version}");
		}
	}
}