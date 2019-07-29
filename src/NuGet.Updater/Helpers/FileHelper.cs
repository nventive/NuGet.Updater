using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Updater.Entities;
using NuGet.Updater.Extensions;
using NuGet.Updater.Log;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

namespace NuGet.Updater.Helpers
{
	public static partial class FileHelper
	{
		public static async Task<Dictionary<UpdateTarget, Dictionary<string, XmlDocument>>> GetTargetFiles(
			CancellationToken ct,
			UpdateTarget updateTarget,
			string solutionRoot,
			Logger log
		)
		{
			var targetFiles = new Dictionary<UpdateTarget, Dictionary<string, XmlDocument>>();

			var allTargets = new[]
			{
				UpdateTarget.Nuspec,
				UpdateTarget.PackageReference,
				UpdateTarget.ProjectJson,
				UpdateTarget.DirectoryProps,
				UpdateTarget.DirectoryTargets,
			};

			foreach(var target in allTargets.Where(t => (updateTarget & t) == t))
			{
				targetFiles.Add(target, await GetFilesForTarget(ct, target, solutionRoot, log));
			}

			return targetFiles;
		}

		private static async Task<Dictionary<string, XmlDocument>> GetFilesForTarget(CancellationToken ct, UpdateTarget target, string solutionRoot, Logger log)
		{
			string extensionFilter = null, nameFilter = null;

			switch(target)
			{
				case UpdateTarget.Nuspec:
					extensionFilter = ".nuspec";
					break;

				case UpdateTarget.ProjectJson:
					nameFilter = "project.json";
					break;

				case UpdateTarget.PackageReference:
					extensionFilter = ".csproj";
					break;

				case UpdateTarget.DirectoryTargets:
					nameFilter = "Directory.Build.targets";
					break;

				case UpdateTarget.DirectoryProps:
					nameFilter = "Directory.Build.props";
					break;

				default:
					break;
			}

			if(extensionFilter == null && nameFilter == null)
			{
				return new Dictionary<string, XmlDocument>();
			}

			log.Write($"Retrieving {nameFilter ?? extensionFilter} files");

			var files = await FileHelper.GetFiles(ct, solutionRoot, extensionFilter, nameFilter);

			log.Write($"Found {files.Length} {nameFilter ?? extensionFilter} file(s)");

			if(target == UpdateTarget.ProjectJson)
			{
				return files.ToDictionary(f => f, f => default(XmlDocument));
			}

			return (await Task.WhenAll(files.Select(f => f.GetDocument(ct))))
				.ToDictionary(p => p.Key, p => p.Value);
		}
	}
}
