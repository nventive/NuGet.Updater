using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using NvGet.Contracts;
using NvGet.Entities;
using NvGet.Tools.Updater.Entities;
using Newtonsoft.Json;
using NuGet.Versioning;
using Uno.Extensions;

namespace NvGet.Tools.Arguments
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
				{ "packageAuthor=|packageAuthors=|a=", "A comma separated list of {authors} of the packages to update; used for public packages only", TrySet(x => context.Parameters.PackageAuthors = x)},
				{ "outputFile=|of=", "The {path} to a markdown file where the update summary will be written", TrySet(x => context.SummaryFile = x) },
				{ "allowDowngrade|d", "Whether package downgrade is allowed", TrySet(x => context.Parameters.IsDowngradeAllowed = true)},
				{ "useNuGetorg|n", "Whether to use packages from NuGet.org", TrySet(_ => context.Parameters.Feeds.Add(PackageFeed.NuGetOrg)) },
				{ "silent", "Suppress all output from NuGet Updater", TrySet(_ => context.IsSilent = true) },
				{ "strict", "Whether to use versions with only the specified version tag (ie. dev, but not dev.test)", TrySet(_ => context.Parameters.Strict = true) },
				{ "dryrun", "Runs the updater but doesn't write the updates to files.", TrySet(_ => context.Parameters.IsDryRun = true) },
				{ "result|r=", "The path to the file where the update result should be saved.", TrySet(x => context.ResultFile = x) },
				{ "versionOverrides=", "The path to a JSON file to force specifc versions to be used; format should be the same as the result file", TryParseAndSet(LoadOverrides, x => context.Parameters.VersionOverrides.AddRange(x)) },
				{ "updateProperties=", "The path to a JSON file that lists pairs of csproj property names and corresponding package Id, so that updater can update project properties as necessary", TryParseAndSet(LoadUpdateProperties, x => context.Parameters.UpdateProperties.AddRange(x)) },
			};

			Action<string> TrySet(Action<string> set)
			{
				return value =>
				{
					if(context != null)
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

		internal static Dictionary<string, (bool, VersionRange)> LoadOverrides(string inputPathOrUrl)
		{
			var results =
				LoadFromStreamAsync()
					.GetAwaiter()
					.GetResult();

			return results.ToDictionary(
				r => r.PackageId,
				r => NuGetVersion.TryParse(r.UpdatedVersion, out var version) ?
					(true, new VersionRange(
						minVersion: version,
						includeMinVersion: true,
						maxVersion: version,
						includeMaxVersion: true,
						floatRange: null,
						originalString: null)) :
							(false, VersionRange.Parse(r.UpdatedVersion)));

			async Task<IEnumerable<UpdateResult>> LoadFromStreamAsync()
			{
				var jsonSerializer = JsonSerializer.CreateDefault();

				if(inputPathOrUrl.StartsWith("http://") || inputPathOrUrl.StartsWith("https://"))
				{
					using(var httpClient = new HttpClient())
					using(var stream = await httpClient.GetStreamAsync(inputPathOrUrl))
					using(var jsonTextReader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8)))
					{
						return jsonSerializer.Deserialize<IEnumerable<UpdateResult>>(jsonTextReader);
					}
				}
				else
				{
					using(var jsonTextReader = new JsonTextReader(File.OpenText(inputPathOrUrl)))
					{
						return jsonSerializer.Deserialize<IEnumerable<UpdateResult>>(jsonTextReader);
					}
				}
			}
		}

		internal static IEnumerable<(string PropertyName, string PackageId)> LoadUpdateProperties(string inputPathOrUrl)
		{
			var results =
				LoadFromStreamAsync()
					.GetAwaiter()
					.GetResult();
			return results;

			async Task<IEnumerable<(string PropertyName, string PackageId)>> LoadFromStreamAsync()
			{
				var jsonSerializer = JsonSerializer.CreateDefault();

				if(inputPathOrUrl.StartsWith("http://") || inputPathOrUrl.StartsWith("https://"))
				{
					using(var httpClient = new HttpClient())
					using(var stream = await httpClient.GetStreamAsync(inputPathOrUrl))
					using(var jsonTextReader = new JsonTextReader(new StreamReader(stream, Encoding.UTF8)))
					{
						return jsonSerializer.Deserialize<IEnumerable<(string PropertyName, string PackageId)>>(jsonTextReader);
					}
				}
				else
				{
					using(var jsonTextReader = new JsonTextReader(File.OpenText(inputPathOrUrl)))
					{
						return jsonSerializer.Deserialize<IEnumerable<PackageProp>>(jsonTextReader).Select(x => (x.PropertyName, x.PackageId));
					}
				}
			}
		}
	}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
	public record PackageProp(string PropertyName, string PackageId);
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
}
