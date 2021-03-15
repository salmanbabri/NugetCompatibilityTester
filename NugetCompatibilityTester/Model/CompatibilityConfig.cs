using System;

namespace NugetCompatibilityTester.Model
{
	public record CompatibilityConfig(string Framework)
	{
		public Version? Version { get; set; }
	};
}