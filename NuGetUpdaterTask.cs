using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nuget.Updater
{
	public class NuGetUpdaterTask : Task
	{
		[Required]
		public string SpecialVersion { get; set; }

		[Required]
		public string[] Packages { get; set; }

		[Required]
		public string SolutionRoot { get; set; }

		[Required]
		public string[] PackageSources { get; set; }

		[Required]
		public string PAT { get; set; }

		public override bool Execute()
		{
			return NuGetUpdater.Execute(Log, SolutionRoot, PackageSources, Packages, SpecialVersion, PAT: PAT);
		}
	}
}
