using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class NugetSdkSearch
	{
		public async Task Search(string packageId, string version)
		{
			var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

			/*
			 var resource = await repository.GetResourceAsync<PackageSearchResource>();
			var searchFilter = new SearchFilter(includePrerelease: false);

			IEnumerable<IPackageSearchMetadata> results = await resource.SearchAsync(
				$"PackageId:{packageId} version:{version}",
				searchFilter,
				skip: 0,
				take: 1,
				NullLogger.Instance,
				CancellationToken.None);

			foreach (IPackageSearchMetadata result in results)
			{
				Console.WriteLine($"Found package {result.Identity.Id} {result.Identity.Version}");
			}
			*/

			/*
			 var dependencyInfoResource = await repository.GetResourceAsync<DependencyInfoResource>();

			var packageIdentity = new PackageIdentity("cake.nuget", NuGetVersion.Parse("0.30.0"));
			var nuGetFramework = NuGetFramework.ParseFolder("net45");

			var dependencyInfo = await dependencyInfoResource.ResolvePackage(packageIdentity,
				nuGetFramework,
				new NullSourceCacheContext(),
				NullLogger.Instance,
				CancellationToken.None);

			Console.WriteLine(dependencyInfo);
			*/

			ILogger logger = NullLogger.Instance;
			var cancellationToken = CancellationToken.None;
			var cache = new SourceCacheContext();

			PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

			IEnumerable<IPackageSearchMetadata> packages = await resource.GetMetadataAsync(
				packageId,
				includePrerelease: false,
				includeUnlisted: false,
				cache,
				logger,
				cancellationToken);

			var ourPackage = packages.FirstOrDefault(p => p.Identity.Version.Equals(NuGetVersion.Parse(version)));

			if (ourPackage is null)
				throw new ArgumentException($"No information available for package: {packageId}, version: {version}");

			var dependencyGroups = ourPackage.DependencySets.ToList();

			bool hasDotNetStandardSupport = IsCompatible(dependencyGroups);

			Console.WriteLine(packages);
		}

		private bool IsCompatible(IReadOnlyCollection<PackageDependencyGroup> dependencyGroups)
		{
			return dependencyGroups.Any(d => d.TargetFramework.Framework.Equals(".NETStandard"));
		}
	}
}