using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Shared.Entities;
using NuGet.Shared.Helpers;
using NuGet.Shared.Log;
using NuGet.Updater.Entities;
using NuGet.Versioning;

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
				string resultFile = default;

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
					{ "dryrun", "Runs the updater but doesn't write the updates to files.", _ => Set(p => p.IsDryRun = true) },
					{ "result|r=", "The path to the file where the update result should be saved.", s => resultFile = s },
					{ "versionOverrides=", "The path to a JSON file to force specifc versions to be used; format should be the same as the result file", s => Set(p => p.VersionOverrides.AddRange(LoadManualOperations(s))) },
				};

				_isParameterSet = false;
				_parameters = new UpdaterParameters
				{
					SolutionRoot = Environment.CurrentDirectory,
					UpdateTarget = FileType.All,
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
					var updater = new NuGetUpdater(_parameters, isSilent ? null : Console.Out, GetSummaryWriter(summaryFile));

					var result = await updater.UpdatePackages(CancellationToken.None);

					Save(result, resultFile);
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

		private static TextWriter GetSummaryWriter(string summaryFile) => summaryFile == null ? null : new SimpleTextWriter(line => FileHelper.LogToFile(summaryFile, line));

		private static void Save(IEnumerable<UpdateResult> result, string path)
		{
			if(path == null || path == "")
			{
				return;
			}

			var serializer = JsonSerializer.CreateDefault();

			using(var writer = File.CreateText(path))
			{
				serializer.Serialize(writer, result);
			}
		}

		private static Dictionary<string, NuGetVersion> LoadManualOperations(string inputFilePath)
		{
			using(var fileReader = File.OpenText(inputFilePath))
			using(var jsonReader = new JsonTextReader(fileReader))
			{
				var result = JsonSerializer.CreateDefault().Deserialize<IEnumerable<UpdateResult>>(jsonReader);

				return result.ToDictionary(r => r.PackageId, r => new NuGetVersion(r.UpdatedVersion));
			}
		}
	}
}
