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

		[Required]
		public string SolutionRoot { get; set; }

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
					packagesToIgnore = new[] { IgnorePackages };
					break;
				default:
					packagesToIgnore = null;
					break;
			}

			return NuGetUpdater.Update(
				SolutionRoot,
				SpecialVersion,
				ExcludeTag,
				PAT: PAT,
				allowDowngrade: AllowDowngrade,
				ignorePackages: packagesToIgnore,
				logAction: message => Log.LogMessage(message)
			);
		}
	}
}
#endif