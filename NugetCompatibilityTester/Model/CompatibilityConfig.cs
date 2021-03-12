using System;

namespace NugetCompatibilityTester
{
	public class CompatibilityConfig
	{
		public string Framework { get; set; }

		public Version? Version { get; set; }
	}
}