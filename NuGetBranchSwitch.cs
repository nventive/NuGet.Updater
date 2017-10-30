using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nuget.Updater
{
    public class NuGetBranchSwitch : Task
	{
		[Required]
		public string SolutionRoot { get; set; }

		[Required]
		public string[] Packages { get; set; }

		[Required]
		public string SourceBranch { get; set; }

		[Required]
		public string TargetBranch { get; set; }

		public override bool Execute() => new NuGetBranchSwitchExecution(Log, SolutionRoot, Packages, SourceBranch, TargetBranch).Execute();
	}
}
