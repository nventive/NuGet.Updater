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

		public override bool Execute()
		{
			return NuGetUpdater.Execute(Log, SolutionRoot, PackageSources, Packages, SpecialVersion);
		}
	}
}
