using System.Collections.Generic;

namespace NuGet.Updater.Entities
{
	public class UpdaterParameters
	{
		/// <summary>
		/// Gets or sets the location of the solution to update.
		/// </summary>
		public string SolutionRoot { get; set; }

		/// <summary>
		/// Gets or sets a list of private feed URLs and access tokens to get packages from.
		/// </summary>
		public Dictionary<string, string> PrivateFeeds { get; set; }

		/// <summary>
		/// Gets or sets the versions to update to (stable, dev, beta, etc.), in order of priority.
		/// </summary>
		public IEnumerable<string> TargetVersions { get; set; }

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
		public UpdateTarget UpdateTarget { get; set; }

		/// <summary>
		/// Gets or sets a list of packages to ignore.
		/// </summary>
		public IEnumerable<string> PackagesToIgnore { get; set; }

		/// <summary>
		/// Gets or sets a list of packages to update; all packages found will be updated if nothing is specified.
		/// </summary>
		public IEnumerable<string> PackagesToUpdate { get; set; }

		/// <summary>
		/// Gets or sets the name of the owner of the packages to update; used with NuGet.org.
		/// </summary>
		public string PackagesOwner { get; set; }
	}
}
