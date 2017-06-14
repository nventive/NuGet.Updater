using System;
using System.Net;
using NuGet;

namespace Nuget.Updater
{
	internal class LocalNugetProvider : ICredentialProvider
	{
		private readonly string PAT;
		private readonly string Email;

		public LocalNugetProvider(string email, string pat)
		{
			Email = email;
			PAT = pat;
		}

		public ICredentials GetCredentials(Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
			=> new System.Net.NetworkCredential(Email, PAT);
	}
}