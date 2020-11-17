using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using NvGet.Contracts;
using NvGet.Entities;
using NvGet.Extensions;
using NvGet.Tools.Hierarchy.Extensions;
using Uno.Extensions;

namespace NvGet.Tools.Hierarchy.Tool
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			try
			{
				var isHelp = false;
				var target = default(string);
				var outputFile = default(string);
				var sources = new List<IPackageFeed>();

				var options = new OptionSet
				{
					{ "help|h", "Displays this help screen", s => isHelp = true },
					{ "solution|s=", "Path to the {solution}", s => target = s },
					{ "outputfile|o=", "Path to a {file} where to write the tree", s => outputFile = s },
					{ "sourceFeed=|sf=", "The NuGet feed from where to download the packages; a private feed can be specified with the format {url|accessToken}", s => sources.Add(PackageFeed.FromString(s)) },
				};

				options.Parse(args);

				if(isHelp)
				{
					Console.WriteLine("NuGet Hierarchy is a tool allowing the view the hierarchy of the NuGet packages found in a solution");
					Console.WriteLine();
					options.WriteOptionDescriptions(Console.Out);
				}

				if(sources.Empty())
				{
					sources.Add(PackageFeed.NuGetOrg);
				}

				var tool = new NuGetHierarchy(target, sources, ConsoleLogger.Instance);

				var result = await tool.RunAsync(CancellationToken.None);

				var reverseReferences = result.GetReversePackageReferences();

				var lines = new List<string>();

				Console.WriteLine();
				Console.WriteLine("Unecessary package references");

				foreach(var r in reverseReferences)
				{
					lines.Add(r.Key);
					Console.WriteLine(r.Key);
					foreach(var p in r.Value)
					{
						var l = $"\t- [{p.Identity}] is referenced by {p.ReferencedBy.Select(i => "[" + i + "]").GetEnumeration()}";
						lines.Add(l);
						Console.WriteLine(l);
					}
				}

				if(outputFile != null)
				{
					var summary = result.GetSummary();
					File.WriteAllLines(outputFile, summary);
					File.WriteAllLines(Path.Combine(Path.GetDirectoryName(outputFile), "unecessary.txt"), lines);
				}
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine($"Failed to download nuget packages: {ex.Message}");
				Console.Error.WriteLine($"{ex}");
			}
		}
	}
}
