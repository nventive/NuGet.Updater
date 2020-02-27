using NeoGet.Entities;

namespace NeoGet.Tools.Downloader.Entities
{
	public class DownloaderResult
	{
		public LocalPackage[] DownloadedPackages { get; set; }

		public LocalPackage[] PushedPackages { get; set; }
	}
}
