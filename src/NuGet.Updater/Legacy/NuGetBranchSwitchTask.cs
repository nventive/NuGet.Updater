#if !UAP && !NETSTANDARD
using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nuget.Updater
{
	public class NuGetBranchSwitchTask : Task
	{
		[Required]
		public string SolutionRoot { get; set; }

		[Required]
		public string[] Packages { get; set; }

		public string SourceBranch { get; set; }

		[Required]
		public string TargetBranch { get; set; }

		public override bool Execute() => new NuGetBranchSwitch(message => Log.LogMessage(message), SolutionRoot, Packages, SourceBranch, TargetBranch).Execute();
	}
}
#endif