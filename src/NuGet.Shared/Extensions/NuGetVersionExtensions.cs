using System;
using System.Linq;
using NuGet.Versioning;

namespace NuGet.Shared.Extensions
{
	public static class NuGetVersionExtensions
	{
		public static bool IsEqualTo(this NuGetVersion x, NuGetVersion y)
		{
			return x == y;
		}

		public static bool IsGreaterThan(this NuGetVersion x, NuGetVersion y)
		{
			if(ReferenceEquals(x, y))
			{
				return false;
			}

			if(ReferenceEquals(y, null))
			{
				return false;
			}

			if(ReferenceEquals(x, null))
			{
				return true;
			}

			// compare version
			var result = x.Major.CompareTo(y.Major);
			if(result != 0)
			{
				// If Major is higher then x is greater.
				return result == 1;
			}

			result = x.Minor.CompareTo(y.Minor);
			if(result != 0)
			{
				// If Minor is higher then x is greater.
				return result == 1;
			}

			result = x.Patch.CompareTo(y.Patch);
			if(result != 0)
			{
				return result == 1;
			}

			// compare release labels
			var xLabels = GetReleaseLabelsOrNull(x);
			var yLabels = GetReleaseLabelsOrNull(y);

			if(xLabels != null
				&& yLabels == null)
			{
				return false;
			}

			if(xLabels == null
				&& yLabels != null)
			{
				return true;
			}

			if(xLabels != null
				&& yLabels != null)
			{
				result = CompareReleaseLabels(xLabels, yLabels);
				if(result != 0)
				{
					return result == 1;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns an array of release labels from the version, or null.
		/// </summary>
		private static string[] GetReleaseLabelsOrNull(SemanticVersion version)
		{
			string[] labels = null;

			// Check if labels exist
			if(version.IsPrerelease)
			{
				// Try to use string[] which is how labels are normally stored.
				var enumerable = version.ReleaseLabels;
				labels = enumerable as string[];

				if(labels != null && enumerable != null)
				{
					// This is not the expected type, enumerate and convert to an array.
					labels = enumerable.ToArray();
				}
			}

			return labels;
		}

		/// <summary>
		/// Compares sets of release labels.
		/// </summary>
		private static int CompareReleaseLabels(string[] xLabels, string[] yLabels)
		{
			var result = 0;

			var count = Math.Max(xLabels.Length, yLabels.Length);

			for(var i = 0; i < count; i++)
			{
				var aExists = i < xLabels.Length;
				var bExists = i < yLabels.Length;

				if(!aExists && bExists)
				{
					return -1;
				}

				if(aExists && !bExists)
				{
					return 1;
				}

				// compare the labels
				result = CompareRelease(xLabels[i], yLabels[i]);

				if(result != 0)
				{
					return result;
				}
			}

			return result;
		}

		/// <summary>
		/// Release labels are compared as numbers if they are numeric, otherwise they will be compared
		/// as strings.
		/// </summary>
		private static int CompareRelease(string version1, string version2)
		{
			var result = 0;

			// check if the identifiers are numeric
			var v1IsNumeric = int.TryParse(version1, out var version1Num);
			var v2IsNumeric = int.TryParse(version2, out var version2Num);

			// if both are numeric compare them as numbers
			if(v1IsNumeric && v2IsNumeric)
			{
				result = version1Num.CompareTo(version2Num);
			}
			else if(v1IsNumeric || v2IsNumeric)
			{
				// numeric labels come before alpha labels
				if(v1IsNumeric)
				{
					result = -1;
				}
				else
				{
					result = 1;
				}
			}
			else
			{
				// If they aren't numeric, don't take a decision.
			}

			return result;
		}
	}
}
