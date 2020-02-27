using System;
using System.Linq;
using NuGet.Configuration;
using Uno.Extensions;

namespace NuGet.Shared.Extensions
{
	public static class PackageSourceExtensions
	{
		private const char PackageFeedInputSeparator = '|';

		/// <summary>
		/// Transforms a input string into a package feed
		/// If the string matches the {url}|{token} format, a private source will be created.
		/// Otherwise, the input will be used as the URL of a public source.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static PackageSource ToPackageSource(this string input)
		{
			var parts = input.Split(PackageFeedInputSeparator);

			var url = parts.ElementAtOrDefault(0);
			var accessToken = parts.ElementAtOrDefault(1);

			if(accessToken == null)
			{
				return new PackageSource(url);
			}

			var sourceName = Guid.NewGuid().ToStringInvariant();

			return new PackageSource(url)
			{
#if WINDOWS_UWP
				Credentials = PackageSourceCredential.FromUserInput(sourceName, "user", accessToken, false),
#else
				Credentials = PackageSourceCredential.FromUserInput(sourceName, "user", accessToken, false, null),
#endif
			};
		}
	}
}
