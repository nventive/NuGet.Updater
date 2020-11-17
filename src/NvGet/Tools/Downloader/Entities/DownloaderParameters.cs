using NeoGet.Contracts;

namespace NeoGet.Tools.Downloader.Entities
{
	public class DownloaderParameters
	{
		/// <summary>
		/// Gets or sets the solution to get packages from.
		/// </summary>
		public string SolutionPath { get; set; }

		/// <summary>
		/// Gets or sets the location where the generate the package cache.
		/// </summary>
		public string PackageOutputPath { get; set; }

		/// <summary>
		/// Gets or sets the feed to use to retrieve the packages.
		/// </summary>
		public IPackageFeed Source { get; set; }

		/// <summary>
		/// Gets or sets the feed where to push the packages.
		/// </summary>
		public IPackageFeed Target { get; set; }
	}
}
