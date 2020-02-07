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
		public static async Task Main(string[] args)
		{
			try
			{
				var (context, options) = ConsoleArgsParser.Parse(args);
				context.Parameters.SolutionRoot ??= Environment.CurrentDirectory;

				if(context.IsHelp)
				{
					Console.WriteLine("NuGet Updater is a tool allowing the automatic update of the NuGet packages found in a solution");
					Console.WriteLine();
					options.WriteOptionDescriptions(Console.Out);
				}
				else
				{
					var updater = new NuGetUpdater(context.Parameters, context.IsSilent ? null : Console.Out, GetSummaryWriter(context.SummaryFile));

					var result = await updater.UpdatePackages(CancellationToken.None);

					Save(result, context.ResultFile);
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
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
	}
}
