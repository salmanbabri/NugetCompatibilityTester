using System.Collections.Generic;

namespace NugetCompatibilityTester
{
	public class CompatibilityInput
	{
		public List<PackageInfo> Packages { get; set; } = new List<PackageInfo>();
	}
}