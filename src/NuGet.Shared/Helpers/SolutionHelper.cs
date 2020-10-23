using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Versioning;
using Uno.Extensions;

namespace NuGet.Shared.Helpers
{
	/// <summary>
	/// Shared solution helper methods.
	/// </summary>
	internal static partial class SolutionHelper
	{
		internal static async Task<PackageReference[]> GetPackageReferences(
			CancellationToken ct,
			string solutionPath,
			FileType fileType,
			ILogger log
		)
		{
			log.LogInformation($"Retrieving references from files in {solutionPath}");

			var packages = new List<PackageReference>();

			if(fileType.HasFlag(FileType.Csproj))
			{
				foreach(var f in await GetProjectFiles(ct, solutionPath, log))
				{
					packages.AddRange(await GetFileReferences(ct, f, FileType.Csproj));
				}
			}

			if(fileType.HasFlag(FileType.DirectoryProps))
			{
				const FileType currentTarget = FileType.DirectoryProps;

				foreach(var file in await GetDirectoryFiles(ct, solutionPath, currentTarget, log))
				{
					packages.AddRange(await GetFileReferences(ct, file, currentTarget));
				}
			}

			if(fileType.HasFlag(FileType.DirectoryTargets))
			{
				const FileType currentTarget = FileType.DirectoryTargets;

				foreach(var file in await GetDirectoryFiles(ct, solutionPath, currentTarget, log))
				{
					packages.AddRange(await GetFileReferences(ct, file, currentTarget));
				}
			}

			if(fileType.HasFlag(FileType.Nuspec))
			{
				foreach(var f in await GetNuspecFiles(ct, solutionPath, log))
				{
					packages.AddRange(await GetFileReferences(ct, f, FileType.Nuspec));
				}
			}

			return packages
				.GroupBy(p => p.Identity)
				.Select(g => new PackageReference(
					g.Key,
					g.SelectMany(p => p.Files).GroupBy(f => f.Key).ToDictionary(f => f.Key, f => f.SelectMany(x => x.Value).Distinct().ToArray())
				))
				.ToArray();
		}

		private static async Task<string[]> GetProjectFiles(CancellationToken ct, string solutionPath, ILogger log)
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
					.Select(m => Path.Combine(solutionFolder, m.Value.Replace('\\', Path.DirectorySeparatorChar)))
					.ToArray();
			}

			log.LogInformation($"Found {files.Length} csproj files");

			return files;
		}

		//To improve: https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2019#search-scope
		//The file should be looked for at all levels
		private static async Task<string[]> GetDirectoryFiles(CancellationToken ct, string solutionPath, FileType target, ILogger log)
		{
			string file;

			if(await FileHelper.IsDirectory(ct, solutionPath))
			{
				var matchingFiles = await FileHelper.GetFiles(ct, solutionPath, nameFilter: target.GetDescription());

				return matchingFiles.ToArray();
			}
			else
			{
				var solutionFolder = Path.GetDirectoryName(solutionPath);
				file = Path.Combine(solutionFolder, target.GetDescription());
			}

			if(file.HasValue() && await FileHelper.Exists(file))
			{
				log.LogInformation($"Found {target.GetDescription()}");
				return new[] { file };
			}

			return new string[0];
		}

		private static async Task<string[]> GetNuspecFiles(CancellationToken ct, string solutionPath, ILogger log)
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

			//Nuspec files are generated in obj when using the new csproj format
			files = files.Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).ToArray();

			log.LogInformation($"Found {files.Length} nuspec files");

			return files;
		}

		private static async Task<PackageReference[]> GetFileReferences(CancellationToken ct, string file, FileType target)
		{
			if(file.IsNullOrEmpty())
			{
				return Array.Empty<PackageReference>();
			}

			var document = await file.LoadDocument(ct);
			var references = Array.Empty<PackageIdentity>();

			if(target.HasAnyFlag(FileType.Csproj, FileType.DirectoryProps, FileType.DirectoryTargets))
			{
				references = document.GetPackageReferences();
			}
			else if(target.HasFlag(FileType.Nuspec))
			{
				references = document.GetDependencies();
			}

			return references
				.GroupBy(r => r.Id)
				.Select(g => new PackageReference(g.Key, new NuGetVersion(g.FirstOrDefault().Version), file, target))
				.ToArray();
		}
	}
}
