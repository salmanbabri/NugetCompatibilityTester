using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugetCompatibilityTester
{
	public class CompatibilityService
	{
		public async Task<List<IPackageSearchMetadata>> GetAllPackageMetadata(string packageId)
		{
			ILogger logger = NullLogger.Instance;
			var cancellationToken = CancellationToken.None;
			var cache = new SourceCacheContext();

			var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);

			var metadata = await resource.GetMetadataAsync(
				packageId,
				includePrerelease: false,
				includeUnlisted: false,
				cache,
				logger,
				cancellationToken);

			return metadata.ToList();
		}
	}
}