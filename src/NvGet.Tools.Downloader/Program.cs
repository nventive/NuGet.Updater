using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using NvGet.Entities;
using NvGet.Tools.Downloader.Entities;
using Uno.Extensions;

namespace NvGet.Tools.Downloader
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			try
			{
				var isHelp = false;
				var parameters = new DownloaderParameters
				{
					PackageOutputPath = GetTemporaryOutputPath(),
				};

				var options = new OptionSet
				{
					{ "help|h", "Displays this help screen", s => isHelp = true },
					{ "solution|s=", "Path to the {solution}", s => parameters.SolutionPath = s },
					{ "output|o=", "Path where to extract the packages; optional, defaults to a temporary folder", s => parameters.PackageOutputPath = s },
					{ "sourceFeed=|sf=", "The NuGet feed from where to download the packages; a private feed can be specified with the format {url|accessToken}", s => parameters.Source = PackageFeed.FromString(s) },
					{ "targetFeed=|tf=", "The NuGet feed where to push the packages; a private feed can be specified with the format {url|accessToken}; optional", s => parameters.Target = PackageFeed.FromString(s) },
				};

				options.Parse(args);

				if(isHelp)
				{
					Console.WriteLine("NuGet Downloader is a tool allowing the download of the NuGet packages found in a solution");
					Console.WriteLine();
					options.WriteOptionDescriptions(Console.Out);
				}

				var result = await NuGetDownloader.RunAsync(CancellationToken.None, parameters, ConsoleLogger.Instance);

				Console.WriteLine($"{result.DownloadedPackages.Length} packages have been downloaded under {parameters.PackageOutputPath}");

				if(parameters.Target != null)
				{
					Console.WriteLine($"{result.PushedPackages?.Length ?? 0} packages have been pushed to {parameters.Target.Url}");
				}
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine($"Failed to download nuget packages: {ex.Message}");
				Console.Error.WriteLine($"{ex}");
			}
		}

		private static string GetTemporaryOutputPath() => Path.Combine(Path.GetTempPath(), $"NuGet.Downloader.{Guid.NewGuid().ToStringInvariant()}");
	}
}
