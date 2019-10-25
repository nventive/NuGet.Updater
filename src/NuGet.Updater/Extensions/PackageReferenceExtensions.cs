using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Shared.Entities;
using NuGet.Shared.Extensions;
using NuGet.Updater.Entities;
using Uno.Extensions;

#if UAP
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
#else
using XmlDocument = System.Xml.XmlDocument;
#endif

namespace NuGet.Updater.Extensions
{
	public static class PackageReferenceExtensions
	{
		/// <summary>
		/// Opens the XML files where package references were found.
		/// </summary>
		/// <param name="references"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static async Task<Dictionary<string, XmlDocument>> OpenFiles(
			this IEnumerable<PackageReference> references,
			CancellationToken ct
		)
		{
			var files = references
				.SelectMany(r => r.Files)
				.SelectMany(g => g.Value)
				.Distinct();

			var documents = new Dictionary<string, XmlDocument>();

			foreach(var file in files)
			{
				documents.Add(file, await file.LoadDocument(ct));
			}

			return documents;
		}

		public static async Task<FeedVersion> GetLatestVersion(
			this PackageReference reference,
			CancellationToken ct,
			UpdaterParameters parameters,
			ILogger log = null
		)
		{
			var availableVersions = await Task.WhenAll(parameters
				.Feeds
				.Select(f => f.GetPackageVersions(ct, reference, parameters.PackageAuthor, log))
			);

			var versionsPerTarget = availableVersions
				.SelectMany(x => x)
				.OrderByDescending(v => v)
				.GroupBy(version => parameters.TargetVersions.FirstOrDefault(t => version.IsMatchingVersion(t, parameters.Strict)))
				.Where(g => g.Key.HasValue());

			return versionsPerTarget
				.Select(g => g.FirstOrDefault())
				.OrderByDescending(v => v.Version)
				.FirstOrDefault();
		}
	}
}
