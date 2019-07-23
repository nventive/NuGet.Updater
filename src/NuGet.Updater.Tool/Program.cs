using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using NuGet.Updater.Entities;

namespace NuGet.Updater.Tool
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			if(args == null || args.Length == 0)
			{
				args = new[] { "help" };
			}

			var parameters = new UpdaterParameters
			{
				UpdateTarget = UpdateTarget.All,
			};

			var isHelp = false;
			string summaryFile = default;

			var options = new OptionSet
			{
				{ "help|h", "Displays this help screen", s => isHelp = true },
				{ "solution=|s=", "The path to the solution to update", s => parameters.SolutionRoot = s },
				{ "feed=|f=", "A private feed to use for the update; the format is {url}|{accessToken}; can be specified multiple times", s => parameters.SourceFeed = s },
				{ "version=|versions=|v=", "The target versions to use", s => parameters.TargetVersions = GetList(s)},
				{ "strict", s => parameters.Strict = GetBoolean(s) },
				{ "excludeTag=|e=", "A tag to exclude from the search", s => parameters.TagToExclude = s },
				{ "useNuGetorg|n", "Whether to pull packages from NuGet.org", _ => parameters.IncludeNuGetOrg = true },
				{ "packagesOwner=|o=", "The owner of the packages to update; must be specified if useNuGetorg is true", s => parameters.PublicPackageOwner = s },
				{ "allowDowngrade=|d=", "Whether package downgrade is allowed", s => parameters.IsDowngradeAllowed = GetBoolean(s) },
				{ "keepLatestDev=|k=", "A comma-separated list of packages to keep at latest dev", s => parameters.PackagesToKeepAtLatestDev = GetList(s) },
				{ "ignore=|i=", "A comma-separated list of packages to ignore", s => parameters.PackagesToIgnore = GetList(s) },
				{ "update=|u=", "A comma-separated list of packages to update; not specifying this will update all packages found", s => parameters.PackagesToUpdate = GetList(s) },
				{ "outputFile=|of=", "The path to a file where the update summary will be written", s => summaryFile = s },
			};

			options.Parse(args);

			if(isHelp)
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
			if(bool.TryParse(value, out var boolean))
			{
				return boolean;
			}

			return fallbackValue;
		}

		private static List<string> GetList(string value)
		{
			var list = new List<string>();

			if(value.Contains(","))
			{
				list.AddRange(value.Split(","));
			}
			else
			{
				list.Add(value);
			}

			return list;
		}
	}
}
