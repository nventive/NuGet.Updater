using System;
using System.Collections.Generic;
using System.Text;

namespace NuGet.Updater.Entities
{
	public class PackageReference
	{
		public PackageReference(string id, string version, string file, UpdateTarget target)
		{
			Id = id;
			Version = version;
			Files = new Dictionary<UpdateTarget, string[]>
			{
				{ target, new[] { file } },
			};
		}

		public PackageReference(string id, string version, Dictionary<UpdateTarget, string[]> files)
		{
			Id = id;
			Version = version;
			Files = files;
		}

		public string Id { get; }

		public string Version { get; }
		
		public Dictionary<UpdateTarget, string[]> Files { get; }

		public override string ToString() => $"{Id} {Version}";
	}
}
