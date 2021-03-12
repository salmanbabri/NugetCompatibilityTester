using System;
using System.Collections.Generic;

namespace NugetCompatibilityTester
{
	public class CompatibilityInput
	{
		public List<PackageInfo> Packages { get; init; } = new();

		public IProgress<string>? Updates { get; set; }
	}
}