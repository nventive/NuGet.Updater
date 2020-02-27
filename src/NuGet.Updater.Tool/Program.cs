using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NeoGet.Helpers;
using NeoGet.Log;
using NeoGet.Tools.Updater;
using NeoGet.Tools.Updater.Arguments;
using NeoGet.Tools.Updater.Entities;
using Newtonsoft.Json;
using Uno.Extensions;

namespace NuGet.Updater.Tool
{
	public static class Program
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
				Console.Error.WriteLine($"Failed to update nuget packages: {ex.Message}");
				Console.Error.WriteLine($"{ex}");
			}
		}

		private static TextWriter GetSummaryWriter(string summaryFile) => summaryFile == null ? null : new SimpleTextWriter(line => FileHelper.LogToFile(summaryFile, line));

		private static void Save(IEnumerable<UpdateResult> result, string path)
		{
			if(path.IsNullOrEmpty())
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
