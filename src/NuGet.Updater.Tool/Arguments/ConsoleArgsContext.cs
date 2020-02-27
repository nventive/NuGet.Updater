using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Options;
using Newtonsoft.Json;
using NuGet.Shared.Entities;
using NuGet.Updater.Entities;
using NuGet.Versioning;
using Uno.Extensions;

namespace NuGet.Updater.Tool.Arguments
{
	public partial class ConsoleArgsContext
	{
		public static ConsoleArgsContext Parse(string[] args)
		{
			if(args.Length == 0)
			{
				return new ConsoleArgsContext { IsHelp = true };
			}

			var context = new ConsoleArgsContext
			{
				Parameters = new UpdaterParameters { UpdateTarget = FileType.All },
			};
			var unparsed = CreateOptionsFor(context).Parse(args);
			context.Errors.AddRange(unparsed.Select(x => new ConsoleArgError(x, ConsoleArgErrorType.UnrecognizedArgument)));

			return context;
		}

		internal static OptionSet CreateOptionsFor(ConsoleArgsContext context = null)
		{
			return new OptionSet
			{
				{ "help|h", "Displays this help screen", TrySet(_ => context.IsHelp = true) },
				{ "solution=|s=", "The {path} to the solution or folder to update; defaults to the current folder", TrySet(x => context.Parameters.SolutionRoot = x) },
				{ "feed=|f=", "A NuGet feed to use for the update; a private feed can be specified with the format {url|accessToken}; can be specified multiple times", TryParseAndSet(PackageFeed.FromString, x => context.Parameters.Feeds.Add(x)) },
				{ "version=|versions=|v=", "The target {version} to use; latest stable is always considered; can be specified multiple times", TrySet(x => context.Parameters.TargetVersions.Add(x)) },
				{ "ignorePackages=|ignore=|i=", "A specific {package} to ignore; can be specified multiple times", TrySet(x => context.Parameters.PackagesToIgnore.Add(x)) },
				{ "updatePackages=|update=|u=", "A specific {package} to update; not specifying this will update all packages found; can be specified multiple times", TrySet(x => context.Parameters.PackagesToUpdate.Add(x)) },
				{ "packageAuthor=|a=", "The {author} of the packages to update; used for public packages only", TrySet(x => context.Parameters.PackageAuthor = x)},
				{ "outputFile=|of=", "The {path} to a markdown file where the update summary will be written", TrySet(x => context.SummaryFile = x) },
				{ "allowDowngrade|d", "Whether package downgrade is allowed", TrySet(x => context.Parameters.IsDowngradeAllowed = true)},
				{ "useNuGetorg|n", "Whether to use packages from NuGet.org", TrySet(_ => context.Parameters.Feeds.Add(PackageFeed.NuGetOrg)) },
				{ "silent", "Suppress all output from NuGet Updater", TrySet(_ => context.IsSilent = true) },
				{ "strict", "Whether to use versions with only the specified version tag (ie. dev, but not dev.test)", TrySet(_ => context.Parameters.Strict = true) },
				{ "dryrun", "Runs the updater but doesn't write the updates to files.", TrySet(_ => context.Parameters.IsDryRun = true) },
				{ "result|r=", "The path to the file where the update result should be saved.", TrySet(x => context.ResultFile = x) },
				{ "versionOverrides=", "The path to a JSON file to force specifc versions to be used; format should be the same as the result file", TryParseAndSet(LoadManualOperations, x => context.Parameters.VersionOverrides.AddRange(x)) },
			};

			Action<string> TrySet(Action<string> set)
			{
				return value =>
				{
					if (context != null)
					{
						try
						{
							set(value);
						}
						catch(Exception e)
						{
							context.Errors.Add(new ConsoleArgError(value, ConsoleArgErrorType.ValueAssignmentError, e));
						}
					}
				};
			}

			Action<string> TryParseAndSet<T>(Func<string, T> parse, Action<T> set)
			{
				return value =>
				{
					if(context != null)
					{
						var isParsing = true;
						try
						{
							var parsed = parse(value);
							isParsing = false;
							set(parsed);
						}
						catch(Exception e)
						{
							context.Errors.Add(new ConsoleArgError(
								value,
								isParsing ? ConsoleArgErrorType.ValueParsingError : ConsoleArgErrorType.ValueAssignmentError,
								e
							));
						}
					}
				};
			}
		}

		public void WriteOptionDescriptions(TextWriter writer) => CreateOptionsFor(default).WriteOptionDescriptions(writer);

		internal static Dictionary<string, NuGetVersion> LoadManualOperations(string inputFilePath)
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
