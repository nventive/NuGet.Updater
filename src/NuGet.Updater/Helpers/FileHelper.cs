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

			var files = await SolutionHelper.GetTargetFilePaths(ct, solutionRoot, target);

			log.Write($"Found {files.Length} {nameFilter ?? extensionFilter} file(s)");

			//All the other supported files are XML files
			if(target == UpdateTarget.ProjectJson)
			{
				return files.ToDictionary(f => f, f => default(XmlDocument));
			}

			//Get the xml document for all the files
			return (await Task.WhenAll(files.Select(f => f.GetDocument(ct))))
				.ToDictionary(p => p.Key, p => p.Value);
		}
	}
}
