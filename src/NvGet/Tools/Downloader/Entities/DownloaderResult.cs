using NvGet.Entities;

namespace NvGet.Tools.Downloader.Entities
{
	public class DownloaderResult
	{
		public LocalPackage[] DownloadedPackages { get; set; }

		public LocalPackage[] PushedPackages { get; set; }
	}
}
