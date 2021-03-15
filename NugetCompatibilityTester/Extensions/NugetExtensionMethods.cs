using System.Collections.Generic;
using System.Linq;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetCompatibilityTester.Extensions
{
	public static class NugetExtensionMethods
	{
		public static bool ApproximatelyEquals(this NuGetVersion version1, NuGetVersion? version2)
		{
			return version2 is not null &&
			       version1.Major == version2.Major &&
			       version1.Minor == version2.Minor;
		}

		public static IPackageSearchMetadata? FindClosestVersion(this IEnumerable<IPackageSearchMetadata> metadata, NuGetVersion? version)
		{
			var list = metadata.ToList();

			return list.FirstOrDefault(p => p.Identity.Version.Equals(version)) ??
			       list.FirstOrDefault(p => p.Identity.Version.ApproximatelyEquals(version));
		}
	}
}