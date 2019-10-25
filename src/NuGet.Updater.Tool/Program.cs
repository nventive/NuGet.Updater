using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using NuGet.Shared.Entities;
using NuGet.Updater.Entities;

namespace NuGet.Updater.Tool
{
	public class Program
	{
		private static bool _isParameterSet = default;
		private static UpdaterParameters _parameters = default;

		public static async Task Main(string[] args)
		{
			try
			{
				var isHelp = false;
				var isSilent = false;
				string summaryFile = default;

				var options = new OptionSet
				{
					{ "help|h", "Displays this help screen", s => isHelp = true },
					{ "solution=|s=", "The {path} to the solution or folder to update; defaults to the current folder", s => Set(p => p.SolutionRoot = s) },
					{ "feed=|f=", "A NuGet feed to use for the update; a private feed can be specified with the format {url|accessToken}; can be specified multiple times", s => Set(p => p.Feeds.Add(PackageFeed.FromString(s)))},
					{ "version=|versions=|v=", "The target {version} to use; latest stable is always considered; can be specified multiple times", s => Set(p => p.TargetVersions.Add(s))},
					{ "ignorePackages=|ignore=|i=", "A specific {package} to ignore; can be specified multiple times", s => Set(p => p.PackagesToIgnore.Add(s)) },
					{ "updatePackages=|update=|u=", "A specific {package} to update; not specifying this will update all packages found; can be specified multiple times", s => Set(p => p.PackagesToUpdate.Add(s)) },
					{ "packageAuthor=|a=", "The {author} of the packages to update; used for public packages only", s => Set(p => p.PackageAuthor = s)},
					{ "outputFile=|of=", "The {path} to a markdown file where the update summary will be written", s => summaryFile = s },
					{ "allowDowngrade|d", "Whether package downgrade is allowed", s => Set(p => p.IsDowngradeAllowed = true)},
					{ "useNuGetorg|n", "Whether to use packages from NuGet.org", _ => Set(p => p.Feeds.Add(PackageFeed.NuGetOrg)) },
					{ "silent", "Suppress all output from NuGet Updater", _ => isSilent = true },
					{ "strict", "Whether to use versions with only the specified version tag (ie. dev, but not dev.test)", _ => Set(p => p.Strict = true) },
				};

				_isParameterSet = false;
				_parameters = new UpdaterParameters
				{
					SolutionRoot = Environment.CurrentDirectory,
					UpdateTarget = FileType.All,
					Feeds = new List<IPackageFeed>(),
					PackagesToIgnore = new List<string>(),
					PackagesToUpdate = new List<string>(),
				};

				options.Parse(args);

				if(isHelp || !_isParameterSet)
				{
					Console.WriteLine("NuGet Updater is a tool allowing the automatic update of the NuGet packages found in a solution");
					Console.WriteLine();
					options.WriteOptionDescriptions(Console.Out);
				}
				else
				{
					var updater = new NuGetUpdater(_parameters, isSilent ? null : Console.Out, summaryFile);

					await updater.UpdatePackages(CancellationToken.None);
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		private static void Set(Action<UpdaterParameters> set)
		{
			set(_parameters);
			_isParameterSet = true;
		}

		private static string[] GetList(string value) => !string.IsNullOrEmpty(value) ? value.Split(",;".ToArray(), StringSplitOptions.RemoveEmptyEntries) : null;
	}
}
