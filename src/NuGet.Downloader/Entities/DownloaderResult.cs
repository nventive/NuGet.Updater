using NuGet.Shared.Entities;

namespace NuGet.Downloader.Entities
{
	public class DownloaderResult
	{
		public LocalPackage[] DownloadedPackages { get; set; }

		public LocalPackage[] PushedPackages { get; set; }
	}
}
