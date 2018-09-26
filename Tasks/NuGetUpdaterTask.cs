#if !UAP
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nuget.Updater
{
	public class NuGetUpdaterTask : Task
	{
		[Required]
		public string SpecialVersion { get; set; }

		public string ExcludeTag { get; set; }

		[Required]
		public string SolutionRoot { get; set; }

		[Required]
		public string PAT { get; set; }
		
		[Required]
		public bool AllowDowngrade { get; set; }

		public override bool Execute()
		{
			return NuGetUpdater.Update(SolutionRoot, SpecialVersion, ExcludeTag, PAT: PAT, allowDowngrade: AllowDowngrade, logAction: message => Log.LogMessage(message));
		}
	}
}
#endif