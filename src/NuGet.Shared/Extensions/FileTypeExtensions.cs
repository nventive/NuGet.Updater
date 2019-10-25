using System.Linq;
using NuGet.Shared.Entities;

namespace NuGet.Shared.Extensions
{
	public static class FileTypeExtensions
	{
		public static string GetDescription(this FileType target)
		{
			switch(target)
			{
				case FileType.Nuspec:
					return ".nuspec";
				case FileType.Csproj:
					return ".csproj";
				case FileType.DirectoryProps:
					return "Directory.Build.targets";
				case FileType.DirectoryTargets:
					return "Directory.Build.props";
				default:
					return default;
			}
		}

		public static bool HasAnyFlag(this FileType target, params FileType[] others)
		{
			return others.Any(t => t.HasFlag(target));
		}
	}
}
