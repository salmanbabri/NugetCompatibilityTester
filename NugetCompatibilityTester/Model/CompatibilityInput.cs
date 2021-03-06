using System;
using System.Collections.Generic;

namespace NugetCompatibilityTester.Model
{
	public record CompatibilityInput(List<PackageInfo> Packages)
	{
		public IProgress<string>? Updates { get; init; }
	}
}