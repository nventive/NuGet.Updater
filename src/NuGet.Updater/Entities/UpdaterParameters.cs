using System.Collections.Generic;
using NuGet.Shared.Entities;

namespace NuGet.Updater.Entities
{
	public class UpdaterParameters
	{
		/// <summary>
		/// Gets or sets the location of the solution to update.
		/// </summary>
		public string SolutionRoot { get; set; }

		/// <summary>
		/// Gets or sets a list of feeds to get packages from.
		/// </summary>
		public ICollection<IPackageFeed> Feeds { get; set; }

		/// <summary>
		/// Gets or sets the versions to update to (stable, dev, beta, etc.), in order of priority.
		/// </summary>
		public ICollection<string> TargetVersions { get; set; } = new List<string> { "stable" };

		/// <summary>
		/// Gets or sets a value indicating whether the version should exactly match the target version.
		/// </summary>
		public bool Strict { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether whether to include packages from NuGet.org.
		/// </summary>
		public bool IncludeNuGetOrg { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether whether the packages can be downgraded if the version found is lower than the current one.
		/// </summary>
		public bool IsDowngradeAllowed { get; set; }

		/// <summary>
		/// Gets or sets the type of files to update.
		/// </summary>
		public FileType UpdateTarget { get; set; }

		/// <summary>
		/// Gets or sets a list of packages to ignore.
		/// </summary>
		public ICollection<string> PackagesToIgnore { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets a list of packages to update; all packages found will be updated if nothing is specified.
		/// </summary>
		public ICollection<string> PackagesToUpdate { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the name of the author of the packages to update; used with NuGet.org; packages from private feeds are assumed to be required.
		/// </summary>
		public string PackageAuthor { get; set; }
	}
}
