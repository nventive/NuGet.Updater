using System;
using NuGet.Packaging.Core;

namespace NuGet.Shared.Entities
{
	[Serializable]
	public class PackageNotFoundException : Exception
	{
		public PackageNotFoundException()
		{
		}

		public PackageNotFoundException(PackageIdentity package, Uri sourceUrl)
			: this($"{package} not found in {sourceUrl.AbsoluteUri}.")
		{
		}

		private PackageNotFoundException(string message) : base(message)
		{
		}

		public PackageNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
