using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester
{
	public class NugetSdkCompatibility
	{
		private readonly CompatibilityService _compatibilityService;
		private readonly CompatibilityAnalyzer _compatibilityAnalyzer;

		public NugetSdkCompatibility(CompatibilityService compatibilityService, CompatibilityAnalyzer compatibilityAnalyzer)
		{
			_compatibilityService = compatibilityService;
			_compatibilityAnalyzer = compatibilityAnalyzer;
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
			var packageMetadata = allMetaData.FindClosestVersion(package.Version);

			if (packageMetadata is null)
				return new CompatibilityInfo(package) { Status = CompatibilityStatus.NotFound };

			CompatibilityInfo info = new(package)
			{
				Status = await _compatibilityAnalyzer.GetStatus(packageMetadata)
			};

			if (!_compatibilityAnalyzer.SupportsModernPlatform(allMetaData.Last()))
				return info;

			var filteredMetadata = info.Status is not CompatibilityStatus.FullyCompatible
				? allMetaData.Where(p => p.Identity.Version > package.Version).ToList()
				: allMetaData;

			return info with
			{
				EarliestCompatible = await FindEarliestCompatibleVersion(filteredMetadata),
				LatestCompatible = await FindLatestCompatibleVersion(allMetaData)
			};
		}

		private async Task<NuGetVersion?> FindEarliestCompatibleVersion(IEnumerable<IPackageSearchMetadata> packageMetadata)
			=> await FindCompatibleVersion(packageMetadata.ToAsyncEnumerable());

		private async Task<NuGetVersion?> FindLatestCompatibleVersion(IEnumerable<IPackageSearchMetadata> allMetaData)
			=> await FindCompatibleVersion(allMetaData.Reverse().ToAsyncEnumerable());

		private async Task<NuGetVersion?> FindCompatibleVersion(IAsyncEnumerable<IPackageSearchMetadata> allMetadata)
		{
			var result = await allMetadata
			                   .SelectAwait(async p => new CompatibilityInfo(p) { Status = await _compatibilityAnalyzer.GetStatus(p) })
			                   .FirstOrDefaultAsync(c => c.Status is CompatibilityStatus.FullyCompatible);

			return result?.Version;
		}
	}
}