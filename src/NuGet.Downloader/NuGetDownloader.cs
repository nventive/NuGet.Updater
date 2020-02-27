using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Downloader.Entities;
using NuGet.Packaging.Core;
using NuGet.Shared.Entities;
using NuGet.Shared.Helpers;
using Uno.Extensions;

namespace NuGet.Downloader
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
			var downloadedPackages = new List<LocalPackage>();

			Directory.CreateDirectory(outputPath);

			foreach(var package in packages)
			{
				var localPackage = await sourceFeed.DownloadPackage(ct, package, outputPath);

				if(localPackage == null)
				{
					throw new PackageNotFoundException(package, sourceFeed.Url); //Shouldn't happen
				}

				downloadedPackages.Add(localPackage);
			}

			return downloadedPackages.ToArray();
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
			var references = await SolutionHelper.GetPackageReferences(ct, solutionPath, FileType.All, _log);

			var identities = new HashSet<PackageIdentity>(references.Select(r => r.Identity));

			return await GetPackagesWithDependencies(ct, identities, source);
		}

		private async Task<HashSet<PackageIdentity>> GetPackagesWithDependencies(
			CancellationToken ct,
			ISet<PackageIdentity> packages,
			IPackageFeed source,
			ISet<PackageIdentity> knownPackages = null,
			ISet<PackageIdentity> missingPackages = null
		)
		{
			knownPackages = knownPackages ?? new HashSet<PackageIdentity>();
			missingPackages = missingPackages ?? new HashSet<PackageIdentity>();

			//Assume we will have to download the packages passed
			var packagesToDonwload = new HashSet<PackageIdentity>(packages);

			foreach(var package in packages)
			{
				try
				{ 
					var packageDependencies = await source.GetDependencies(ct, package);

					_log.LogInformation($"Found {packageDependencies.Length} dependencies for {package} in {source.Url}");

					//TODO: make the version used parametrable (Use MaxVersion or MinVersion)
					packagesToDonwload.AddRange(packageDependencies.Select(d => new PackageIdentity(d.Id, d.VersionRange.MinVersion)));

					knownPackages.Add(package);
				}
				catch(PackageNotFoundException ex)
				{
					_log.LogInformation(ex.Message);

					missingPackages.Add(package);
				}
			}

			//Take all the dependencies found
			var unknownPackages = new HashSet<PackageIdentity>(packagesToDonwload);
			//Remove the packages that we already know are missing
			unknownPackages.ExceptWith(missingPackages); 
			//Remove the packages we already have dependencies for
			unknownPackages.ExceptWith(knownPackages);

			if(unknownPackages.Any())
			{
				//Get the dependencies for the rest
				var subDependencies = await GetPackagesWithDependencies(ct, unknownPackages, source, knownPackages, missingPackages);
				//Add those to the packages to download
				packagesToDonwload.UnionWith(subDependencies);
			}

			//Remove the packages that we know are missing
			packagesToDonwload.ExceptWith(missingPackages);

			return packagesToDonwload;
		}
	}
}
