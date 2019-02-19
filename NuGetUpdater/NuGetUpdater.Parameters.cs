using System;
using System.Collections.Generic;
using System.Linq;
using Nuget.Updater.Entities;
using NuGet.Configuration;

namespace Nuget.Updater
{
	partial class NuGetUpdater
	{
		public class Parameters
		{
			/// <summary>
			/// The location of the solution to update.
			/// </summary>
			public string SolutionRoot { get; set; }

			/// <summary>
			/// The URL of the private feed to use.
			/// </summary>
			public string SourceFeed { get; set; }

			/// <summary>
			/// The Personal Access Token to use to access the private feed.
			/// </summary>
			public string SourceFeedPersonalAccessToken { get; set; }

			/// <summary>
			/// The target version for the update (stable, dev, beta, etc.)
			/// </summary>
			public string TargetVersion { get; set; }

			/// <summary>
			/// Whether it should exactly match the target version.
			/// </summary>
			public bool Strict { get; set; }

			/// <summary>
			/// A specific tag to exclude when looking for versions.
			/// </summary>
			public string TagToExclude { get; set; }

			/// <summary>
			/// Whether to include packages from NuGet.org
			/// </summary>
			public bool IncludeNuGetOrg { get; set; }

			/// <summary>
			/// Whether the packages can be downgraded if the version found is lower.
			/// </summary>
			public bool IsDowngradeAllowed { get; set; }

			/// <summary>
			/// The type of files with NuGet references to update.
			/// </summary>
			public UpdateTarget UpdateTarget { get; set; }

			/// <summary>
			/// A list of packages to keep at latest dev.
			/// </summary>
			public IEnumerable<string> PackagesToKeepAtLatestDev { get; set; }

			/// <summary>
			/// A list of packages to ignore.
			/// </summary>
			public IEnumerable<string> PackagesToIgnore { get; set; }

			/// <summary>
			/// A list of packages to update. Will update all packages found if nothing is specified.
			/// </summary>
			public IEnumerable<string> PackagesToUpdate { get; set; }

			/// <summary>
			/// Whether to use the stable version if a more recent version is available
			/// </summary>
			public bool UseStableIfMoreRecent { get; set; }
		}
	}
}
