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
		private static readonly UpdaterParameters _parameters = new UpdaterParameters
		{
			UpdateTarget = UpdateTarget.All,
			PrivateFeeds = new Dictionary<string, string>(),
		};

		public static async Task Main(string[] args)
		{
			var isHelp = false;
			var isSilent = false;
			string summaryFile = default;

			var options = new OptionSet
			{
				{ "help|h", "Displays this help screen", s => isHelp = true },
				{ "solution=|s=", "The path to the solution to update", s => _parameters.SolutionRoot = s },
				{ "feed=|f=", "A private feed to use for the update; the format is {url}|{accessToken}; can be specified multiple times", s => ParsePrivateFeed(s, '|') },
				{ "version=|versions=|v=", "The target versions to use", s => _parameters.TargetVersions = GetList(s)},
				{ "useNuGetorg|n", "Whether to pull packages from NuGet.org", _ => _parameters.IncludeNuGetOrg = true },
				{ "packagesOwner=|o=", "The owner of the packages to update; must be specified if useNuGetorg is true", s => _parameters.PackagesOwner = s },
				{ "allowDowngrade=|d=", "Whether package downgrade is allowed", s => _parameters.IsDowngradeAllowed = GetBoolean(s) },
				{ "ignore=|i=", "A comma-separated list of packages to ignore", s => _parameters.PackagesToIgnore = GetList(s) },
				{ "update=|u=", "A comma-separated list of packages to update; not specifying this will update all packages found", s => _parameters.PackagesToUpdate = GetList(s) },
				{ "outputFile=|of=", "The path to a file where the update summary will be written", s => summaryFile = s },
				{ "silent", "Suppress all output from NuGet Updater", _ => isSilent = true },
			};

			if(options.Parse(args).Count == 0)
			{
				isHelp = true;
			}

			if(isHelp)
			{
				Console.WriteLine("NuGet Updater is a tool allowing the automatic update of the NuGet packages found in a solution");
				options.WriteOptionDescriptions(Console.Out);
			}
			else
			{
				await NuGetUpdater.UpdateAsync(CancellationToken.None, _parameters, isSilent ? null : Console.Out, summaryFile);
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

		private static void ParsePrivateFeed(string value, char separator)
		{
			if(value.Contains(separator))
			{
				var parts = value.Split(separator);
				_parameters.PrivateFeeds.Add(parts[0], parts[1]);
			}
			else
			{
				_parameters.PrivateFeeds.Add(value, null);
			}
		}
	}
}
