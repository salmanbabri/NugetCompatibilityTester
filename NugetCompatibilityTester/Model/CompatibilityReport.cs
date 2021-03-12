using System;
using System.Collections.Generic;

namespace NugetCompatibilityTester
{
	public class CompatibilityReport
	{
		public CompatibilityConfig Config { get; set; }

		public List<CompatibilityInfo> CompatibilityDetails { get; set; }

		public TimeSpan TimeToExecute { get; set; }
	}
}