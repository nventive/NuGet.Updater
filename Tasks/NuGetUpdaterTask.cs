#if !UAP && !NETSTANDARD
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Nuget.Updater
{
	public class NuGetUpdaterTask : Task
	{
		[Required]
		public string SpecialVersion { get; set; }

		[Required]
		public string SolutionRoot { get; set; }

		[Required]
		public string NuGetFeed { get; set; }

		[Required]
		public string PAT { get; set; }
		
		[Required]
		public bool AllowDowngrade { get; set; }

		public string ExcludeTag { get; set; }

		public string IgnorePackages { get; set; }

		public string UpdatePackages { get; set; }

		public string UpdateSummaryFile { get; set; }

		public bool UseStableIfMoreRecent { get; set; }

		public override bool Execute()
		{
			var parameters = new NuGetUpdater.Parameters
			{
				SolutionRoot = SolutionRoot,
				SourceFeed = NuGetFeed,
				SourceFeedPersonalAccessToken = PAT,
				TargetVersion = SpecialVersion,
				IsDowngradeAllowed = AllowDowngrade,
				PackagesToIgnore = GetPackages(IgnorePackages),
				PackagesToUpdate = GetPackages(UpdatePackages),
				TagToExclude = ExcludeTag,
				UseStableIfMoreRecent = UseStableIfMoreRecent,
				//Default values
				PackagesToKeepAtLatestDev = new string[0],
				IncludeNuGetOrg = true,
				Strict = true,
				UpdateTarget = UpdateTarget.All,
			};

			return NuGetUpdater.Update(
				parameters,
				message => Log.LogMessage(message),
				UpdateSummaryFile
			);
		}

		private string[] GetPackages(string input)
		{
			switch (input)
			{
				case var p when p == null:
					return null;
				case var p when p.Contains(";"):
					return p.Split(';');
				case var p when p != null && p != "":
					return new[] { p };
				default:
					return null;
			}
		}
	}
}
#endif