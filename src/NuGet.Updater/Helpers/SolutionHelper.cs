using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;
using Uno.Extensions;

namespace NuGet.Updater.Helpers
{
	public static class SolutionHelper
	{
		internal static async Task<PackageReference[]> GetPackageReferences(
			CancellationToken ct,
			string solutionPath,
			UpdateTarget updateTarget,
			Logger log = null
		)
		{
			log?.Write($"Retrieving references from files in {solutionPath}");

			var packages = new List<PackageReference>();

			if(updateTarget.Matches(UpdateTarget.Csproj))
			{
				foreach(var f in await GetProjectFiles(ct, solutionPath, log))
				{
					packages.AddRange(await GetFileReferences(ct, f, UpdateTarget.Csproj));
				}
			}

			if(updateTarget.Matches(UpdateTarget.DirectoryProps))
			{
				packages.AddRange(await GetFileReferences(ct, GetDirectoryFile(solutionPath, UpdateTarget.DirectoryProps, log), UpdateTarget.DirectoryProps));
			}

			if(updateTarget.Matches(UpdateTarget.DirectoryTargets))
			{
				packages.AddRange(await GetFileReferences(ct, GetDirectoryFile(solutionPath, UpdateTarget.DirectoryTargets, log), UpdateTarget.DirectoryTargets));
			}

			return packages
				.GroupBy(p => p.Id)
				.Select(g => new PackageReference(
					g.Key,
					g.Select(p => p.Version).OrderByDescending(v => v).FirstOrDefault(),
					g.SelectMany(p => p.Files).GroupBy(f => f.Key).ToDictionary(f => f.Key, f => f.SelectMany(x => x.Value).Distinct().ToArray())
				))
				.ToArray();
		}

		private static async Task<string[]> GetProjectFiles(CancellationToken ct, string solutionPath, Logger log = null)
		{
			var solutionContent = await FileHelper.ReadFileContent(ct, solutionPath);
			var solutionFolder = Path.GetDirectoryName(solutionPath);

			var matches = Regex.Matches(solutionContent, "[^\\s\"]*\\.csproj");

			var files = matches
				.Cast<Match>()
				.Select(m => Path.Combine(solutionFolder, m.Value))
				.ToArray();

			log?.Write($"Found {files.Length} csproj files");

			return files;
		}

		private static string GetDirectoryFile(string solutionPath, UpdateTarget target, Logger log = null)
		{
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var file = Path.Combine(solutionFolder, target.GetDescription());

			if(File.Exists(file))
			{
				log?.Write($"Found {target.GetDescription()}");
				return file;
			}

			return null;
		}

		private static async Task<PackageReference[]> GetFileReferences(CancellationToken ct, string file, UpdateTarget target)
		{
			if(file.IsNullOrEmpty())
			{
				return new PackageReference[0];
			}

			var document = await file.GetDocument(ct);

			var references = document.GetPackageReferences();

			return references
				.Select(g => new PackageReference(g.Key, g.Value, file, target))
				.ToArray();
		}
	}
}
