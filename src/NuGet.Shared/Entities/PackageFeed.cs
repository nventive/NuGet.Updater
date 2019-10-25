using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Shared.Extensions;
using Uno.Extensions;

namespace NuGet.Shared.Entities
{
	public class PackageFeed : IPackageFeed
	{
		#region Static
		private const char PackageFeedInputSeparator = '|';
		public static readonly PackageFeed NuGetOrg = new PackageFeed("https://api.nuget.org/v3/index.json");

		public static PackageFeed FromString(string input)
		{
			var parts = input.Split(PackageFeedInputSeparator);

			return parts.Length == 1
				? new PackageFeed(parts[0])
				: new PackageFeed(parts[0], parts[1]);
		}
		#endregion

		private readonly PackageSource _packageSource;
		private readonly bool _isPrivate = false;

		private PackageFeed(string url)
			: this(new PackageSource(url))
		{
		}

		private PackageFeed(string url, string accessToken)
			: this(GetPackageSource(url, accessToken), isPrivate: true)
		{
		}

		private PackageFeed(PackageSource source, bool isPrivate = false)
		{
			_packageSource = source;
			IsPrivate = isPrivate;
		}

		public Uri Url => _packageSource.SourceUri;

		public bool IsPrivate { get; }

		public async Task<FeedVersion[]> GetPackageVersions(
			CancellationToken ct,
			PackageReference reference,
			string author = null,
			ILogger log = null
		)
		{
			var logMessage = new StringBuilder();

			logMessage.AppendLine($"Retrieving package {reference.Identity.Id} from {Url}");

			var versions = await _packageSource.GetPackageVersions(ct, reference.Identity.Id);

			logMessage.AppendLine(versions.Length > 0 ? $"Found {versions.Length} versions" : "No versions found");

			if(author.HasValue() && versions.Any())
			{
				versions = versions.Where(m => m.HasAuthor(author)).ToArray();

				logMessage.AppendLine(versions.Length > 0 ? $"Found {versions.Length} versions from {author}" : $"No versions from {author} found");
			}

			log?.LogInformation(logMessage.ToString());

			return versions
				.Cast<PackageSearchMetadataRegistration>()
				.Select(m => new FeedVersion(m.Version, Url))
				.ToArray();
		}

		private static PackageSource GetPackageSource(string url, string accessToken)
		{
			var name = url.GetHashCode().ToStringInvariant();

			return new PackageSource(url, name)
			{
#if UAP
				Credentials = PackageSourceCredential.FromUserInput(name, "user", accessToken, false),
#else
				Credentials = PackageSourceCredential.FromUserInput(name, "user", accessToken, false, null),
#endif
			};
		}
	}
}
