using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using Uno.Extensions;

namespace NuGet.Updater.Helpers
{
	public static class SolutionHelper
	{
		internal static async Task<string[]> GetTargetFilePaths(CancellationToken ct, string solutionPath, UpdateTarget updateTarget)
		{
			switch(updateTarget)
			{
				case UpdateTarget.PackageReference:
					return await GetProjectFiles(ct, solutionPath);
				case UpdateTarget.DirectoryProps:
				case UpdateTarget.DirectoryTargets:
					return new[] { GetDirectoryFile(solutionPath, updateTarget) };
				case UpdateTarget.Nuspec:
					return new string[0]; // find all matching files in the solution folder
				case UpdateTarget.ProjectJson:
					return new string[0]; //find all csproj, look for project.json next to them
				case UpdateTarget.Unspecified:
				case UpdateTarget.All:
				default:
					return new string[0];
			}
		}

		internal static async Task<PackageReference[]> GetPackageReferences(CancellationToken ct, string solutionPath)
		{
			var packages = new List<(string path, string package, string version)>();

			var files = await GetTargetFilePaths(ct, solutionPath, UpdateTarget.PackageReference);

			foreach(var f in files)
			{
				var document = (await f.GetDocument(ct)).Value;

				var references = document.GetPackageReferences();

				packages.AddRange(references.Select(g => (path: f, package: g.Key, version: g.Value)));
			}

			return packages
				.GroupBy(x => x.package)
				.Select(g => g
					.GroupBy(x => x.version)
					.Select(v => new PackageReference
					{
						Id = g.Key,
						Version = v.Key,
						Files = v.Select(x => x.path).ToArray(),
					})
				)
				.SelectMany(x => x)
				.ToArray();
		}

		private static async Task<string[]> GetProjectFiles(CancellationToken ct, string solutionPath)
		{
			var solutionContent = await FileHelper.ReadFileContent(ct, solutionPath);
			var solutionFolder = Path.GetDirectoryName(solutionPath);

			var matches = Regex.Matches(solutionContent, "[^\\s\"]*\\.csproj");

			return matches
				.Cast<Match>()
				.Select(m => Path.GetDirectoryName(solutionPath))
				.ToArray();
		}

		private static string GetDirectoryFile(string solutionPath, UpdateTarget target)
		{
			var solutionFolder = Path.GetDirectoryName(solutionPath);
			var file = Path.Combine(solutionFolder, target.GetDescription());

			if(File.Exists(file))
			{
				return file;
			}

			return null;
		}
	}
}
