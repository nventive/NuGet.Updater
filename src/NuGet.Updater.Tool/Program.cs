using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

using UpdaterParameters = NuGet.Updater.NuGetUpdater.Parameters;

namespace NuGet.Updater.Tool
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var parameters = new UpdaterParameters
			{
				UpdateTarget = UpdateTarget.All
			};

			var isHelp = false;
			string summaryFile = default;

			var options = new OptionSet
			{
				{ "help", "Displays this help screen", s => isHelp = true },
				{ "solution=", "The path to the solution to update", s => parameters.SolutionRoot = s },
				{ "feed=", "The URL of a private NuGet feed to use", s => parameters.SourceFeed = s },
				{ "pat=", "The PAT used to authenticate to the private NuGet feed", s => parameters.SourceFeedPersonalAccessToken = s },
				{ "version=", "The target version to use", s => parameters.TargetVersion = s },
				{ "strict=", s => parameters.Strict = GetBoolean(s) },
				{ "excludeTag=", "A tag to exclude from the search", s => parameters.TagToExclude = s },
				{ "useNuGetorg=", "Whether to pull packages from NuGet.org", s => parameters.IncludeNuGetOrg = GetBoolean(s) },
				{ "publicPackagesOwner=", "The owner of the public packages to update; must be specified is useNuGetorg is true", s => parameters.PublickPackageOwner = s },
				{ "allowDowngrade=", "Whether package downgrade is allowed", s => parameters.IsDowngradeAllowed = GetBoolean(s) },
				{ "keepLatestDev=", "A comma-separated list of packages to keep at latest dev", s => parameters.PackagesToKeepAtLatestDev = GetList(s) },
				{ "ignore=", "A comma-separated list of packages to ignore", s => parameters.PackagesToIgnore = GetList(s) },
				{ "update=", "A comma-separated list of packages to update; not specifying this will update all packages found", s => parameters.PackagesToUpdate = GetList(s) },
				{ "useStableIfMoreRecent=", "Whether to use the latest stable if a more recent version is found", s => parameters.UseStableIfMoreRecent = GetBoolean(s) },
				{ "outputFile=", "The path to a file where the update summary will be written", s => summaryFile = s },
			};

			options.Parse(args);

			if (isHelp)
			{
				Console.WriteLine("NuGet Updater is a tool allowing the automatic update of the NuGet packages found in a solution");
				options.WriteOptionDescriptions(Console.Out);
			}
			else
			{
				await NuGetUpdater.UpdateAsync(CancellationToken.None, parameters, Console.Out, summaryFile);
			}
		}

		private static bool GetBoolean(string value, bool fallbackValue = false)
		{
			if (bool.TryParse(value, out var boolean))
			{
				return boolean;
			}

			return fallbackValue;
		}

		private static List<string> GetList(string value)
		{
			var list = new List<string>();

			if (value.Contains(","))
			{
				list.AddRange(value.Split(","));
			}

			return list;
		}
	}
}
