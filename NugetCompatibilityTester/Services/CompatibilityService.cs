using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugetCompatibilityTester.Services
{
	public class CompatibilityService
	{
		private readonly Task<PackageMetadataResource> _resourceTask;

		public CompatibilityService()
		{
			var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
			_resourceTask = repository.GetResourceAsync<PackageMetadataResource>(CancellationToken.None);
		}

		public async Task<List<IPackageSearchMetadata>> GetAllPackageMetadata(string packageId)
		{
			ILogger logger = NullLogger.Instance;
			var cancellationToken = CancellationToken.None;
			var cache = new SourceCacheContext();

			var resource = await _resourceTask;

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