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
	/// <summary>
	/// Shared solution helper methods.
	/// </summary>
	public static partial class SolutionHelper
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

			if(updateTarget.HasFlag(UpdateTarget.Csproj))
			{
				foreach(var f in await GetProjectFiles(ct, solutionPath, log))
				{
					packages.AddRange(await GetFileReferences(ct, f, UpdateTarget.Csproj));
				}
			}

			if(updateTarget.HasFlag(UpdateTarget.DirectoryProps))
			{
				const UpdateTarget currentTarget = UpdateTarget.DirectoryProps;

				var file = await GetDirectoryFile(ct, solutionPath, currentTarget, log);
				packages.AddRange(await GetFileReferences(ct, file, currentTarget));
			}

			if(updateTarget.HasFlag(UpdateTarget.DirectoryTargets))
			{
				const UpdateTarget currentTarget = UpdateTarget.DirectoryTargets;

				var file = await GetDirectoryFile(ct, solutionPath, currentTarget, log);
				packages.AddRange(await GetFileReferences(ct, file, currentTarget));
			}

			if(updateTarget.HasFlag(UpdateTarget.Nuspec))
			{
				var files = await GetNuspecFiles(ct, solutionPath, log);
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
			var files = new string[0];

			if(await FileHelper.IsDirectory(ct, solutionPath))
			{
				files = await FileHelper.GetFiles(ct, solutionPath, extensionFilter: ".csproj");
			}
			else
			{
				var solutionContent = await FileHelper.ReadFileContent(ct, solutionPath);
				var solutionFolder = Path.GetDirectoryName(solutionPath);

				files = Regex
					.Matches(solutionContent, "[^\\s\"]*\\.csproj")
					.Cast<Match>()
					.Select(m => Path.Combine(solutionFolder, m.Value))
					.ToArray();
			}

			log?.Write($"Found {files.Length} csproj files");

			return files;
		}

		//To improve: https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2019#search-scope
		//The file should be looked for at all levels
		private static async Task<string> GetDirectoryFile(CancellationToken ct, string solutionPath, UpdateTarget target, Logger log = null)
		{
			string file;

			if(await FileHelper.IsDirectory(ct, solutionPath))
			{
				var matchingFiles = await FileHelper.GetFiles(ct, solutionPath, nameFilter: target.GetDescription());

				file = matchingFiles.SingleOrDefault();
			}
			else
			{
				var solutionFolder = Path.GetDirectoryName(solutionPath);
				file = Path.Combine(solutionFolder, target.GetDescription());
			}

			if(file != null && File.Exists(file))
			{
				log?.Write($"Found {target.GetDescription()}");
				return file;
			}

			return null;
		}

		private static async Task<string[]> GetNuspecFiles(CancellationToken ct, string solutionPath, Logger log = null)
		{
			string solutionFolder;

			if(await FileHelper.IsDirectory(ct, solutionPath))
			{
				solutionFolder = solutionPath;
			}
			else
			{
				solutionFolder = Path.GetDirectoryName(solutionPath);
			}

			var files = await FileHelper.GetFiles(ct, solutionFolder, extensionFilter: ".nuspec");

			log?.Write($"Found {files.Length} nuspec files");

			return files;
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
