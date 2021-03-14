using System;

namespace NugetCompatibilityTester
{
	public record CompatibilityConfig(string Framework)
	{
		public Version? Version { get; set; }
	};
}