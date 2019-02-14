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

		public string IgnorePackages { get; set; }

		public string UpdatePackages { get; set; }

		public string UpdateSummaryFile { get; set; }

		[Required]
		public string SolutionRoot { get; set; }

		[Required]
		public string NuGetFeed { get; set; }

		[Required]
		public string PAT { get; set; }
		
		[Required]
		public bool AllowDowngrade { get; set; }

		public override bool Execute()
		{
			string[] packagesToIgnore;

			switch (IgnorePackages)
			{
				case var p when p == null:
					packagesToIgnore = new string[0];
					break;
				case var p when p.Contains(";"):
					packagesToIgnore = p.Split(';');
					break;
				case var p when p != null:
					packagesToIgnore = new[] { p };
					break;
				default:
					packagesToIgnore = null;
					break;
			}

			string[] packagesToUpdate;

			switch (UpdatePackages)
			{
				case var p when p == null:
					packagesToUpdate = new string[0];
					break;
				case var p when p.Contains(";"):
					packagesToUpdate = p.Split(';');
					break;
				case var p when p != null:
					packagesToUpdate = new[] { p };
					break;
				default:
					packagesToUpdate = null;
					break;
			}

			return NuGetUpdater.Update(
				SolutionRoot,
				NuGetFeed,
				SpecialVersion,
				ExcludeTag,
				PAT: PAT,
				allowDowngrade: AllowDowngrade,
				ignorePackages: packagesToIgnore,
				updatePackages: packagesToUpdate,
				logAction: message => Log.LogMessage(message)
			);
		}
	}
}
#endif