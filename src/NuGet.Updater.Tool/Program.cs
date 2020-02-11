using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Shared.Helpers;
using NuGet.Shared.Log;
using NuGet.Updater.Entities;
using NuGet.Updater.Tool.Arguments;

namespace NuGet.Updater.Tool
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			try
			{
				var context = ConsoleArgsContext.Parse(args);

				if (context.HasError)
				{
					Console.Error.WriteLine(context.Errors.FirstOrDefault().Message);
					Environment.Exit(-1);
				}
				else if (context.IsHelp)
				{
					Console.WriteLine("NuGet Updater is a tool allowing the automatic update of the NuGet packages found in a solution");
					Console.WriteLine();
					context.WriteOptionDescriptions(Console.Out);
				}
				else
				{
					context.Parameters.SolutionRoot ??= Environment.CurrentDirectory;
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
