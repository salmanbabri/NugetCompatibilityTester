using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class NugetSdkSearch
	{
		public async Task Search(string packageId, string version)
		{
			bool hasDotNetStandardSupport = await IsCompatible(packageId, NuGetVersion.Parse(version));

			Console.WriteLine(hasDotNetStandardSupport);
		}

		private async Task<bool> IsCompatible(string packageId, NuGetVersion version)
		{
			var packagesMetadata = await GetPackageMetadata(packageId);

			var currentPackage = packagesMetadata.FirstOrDefault(p => p.Identity.Version.Equals(version));

			if (currentPackage is null)
				throw new ArgumentException($"No information available for package: {packageId}, version: {version}");

			var dependencyGroups = currentPackage.DependencySets.ToList();

			if (dependencyGroups.Count is 0)
				return false;

			if (dependencyGroups.Any(d => d.TargetFramework.Framework.Equals(".NETStandard")))
				return true;

			var otherDependencies = dependencyGroups.Where(d => d.TargetFramework == NuGetFramework.AnyFramework)
			                                        .SelectMany(d => d.Packages)
			                                        .ToList();

			foreach (var package in otherDependencies)
			{
				bool isCompatible = await IsCompatible(package.Id, package.VersionRange.MinVersion);
				if (isCompatible)
					return true;
			}

			return false;
		}

		private async Task<IEnumerable<IPackageSearchMetadata>> GetPackageMetadata(string packageId)
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