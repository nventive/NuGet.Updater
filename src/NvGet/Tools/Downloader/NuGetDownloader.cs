using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NvGet.Contracts;
using NvGet.Entities;
using NvGet.Tools.Downloader.Entities;
using NvGet.Tools.Hierarchy;
using NvGet.Tools.Hierarchy.Extensions;
using NuGet.Common;
using NuGet.Packaging.Core;
using Uno.Extensions;

namespace NvGet.Tools.Downloader
{
	public class NuGetDownloader
	{
		public static async Task<DownloaderResult> RunAsync(CancellationToken ct, DownloaderParameters parameters, ILogger log)
		{
			var downloader = new NuGetDownloader(log);

			return await downloader.RunAsync(ct, parameters);
		}

		private readonly ILogger _log;

		private NuGetDownloader(ILogger log)
		{
			_log = log;
		}

		public async Task<DownloaderResult> RunAsync(CancellationToken ct, DownloaderParameters parameters)
		{
			var stopwatch = Stopwatch.StartNew();

			var result = new DownloaderResult();

			var packages = await GetPackagesToDownload(ct, parameters.SolutionPath, parameters.Source);

			_log.LogInformation($"Found {packages.Count()} packages to download");

			result.DownloadedPackages = await DownloadPackages(ct, packages, parameters.Source, parameters.PackageOutputPath);
			result.PushedPackages = await PushPackages(ct, result.DownloadedPackages, parameters.Target);

			stopwatch.Stop();

			_log.LogInformation($"Operation completed in {stopwatch.Elapsed}");

			return result;
		}

		private async Task<LocalPackage[]> DownloadPackages(
			CancellationToken ct,
			IEnumerable<PackageIdentity> packages,
			IPackageFeed sourceFeed,
			string outputPath
		)
		{
			Directory.CreateDirectory(outputPath);

			var downloadedPackages = await Task.WhenAll(packages.Select(package => sourceFeed.DownloadPackage(ct, package, outputPath)));

			return downloadedPackages.Trim().ToArray();
		}

		private async Task<LocalPackage[]> PushPackages(CancellationToken ct, IEnumerable<LocalPackage> packages, IPackageFeed targetFeed)
		{
			var pushedPackages = new List<LocalPackage>();

			if(targetFeed != null)
			{
				_log.LogInformation($"Pushing packages to {targetFeed.Url}");

				foreach(var package in packages)
				{
					if(await targetFeed.PushPackage(ct, package))
					{
						pushedPackages.Add(package);
					}
				}
			}

			return pushedPackages.ToArray();
		}

		private async Task<IEnumerable<PackageIdentity>> GetPackagesToDownload(CancellationToken ct, string solutionPath, IPackageFeed source)
		{
			var hierachy = new NuGetHierarchy(solutionPath, new[] { source }, _log);

			var result = await hierachy.RunAsync(ct);

			return result.GetAllIdentities();
		}
	}
}
