using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;

namespace NuGet.Shared.Helpers
{
	public static class MarkdownHelper
	{
		internal static string Link(string text, string url) => url.HasValue() ? $"[{text}]({url})" : text;

		internal static string Bold(string text) => $"**{text}**";

		internal static string CodeBlocksEnumeration(IEnumerable<string> values) => StringHelper.Enumeration(values.Select(v => CodeBlock(v)));

		internal static string CodeBlock(Uri url, bool isMultiline = false, string language = null)
			=> CodeBlock(url.OriginalString, isMultiline, language);

		internal static string CodeBlock(string text, bool isMultiline = false, string language = null)
		{
			if(isMultiline)
			{
				return new StringBuilder()
					.AppendLine($"```{language}")
					.AppendLine(text)
					.AppendLine("```")
					.ToString();
			}

			return $"`{text}`";
		}

		internal class TableBuilder
		{
			private readonly List<string> _header = new List<string>();
			private readonly List<string[]> _body = new List<string[]>();

			public void AddHeader(string text) => _header.Add(text);

			public void AddLine(params string[] columns) => _body.Add(columns.Trim().ToArray());

			public string Build()
			{
				var builder = new StringBuilder();

				if(_header.Any())
				{
					builder
						.AppendLine($"|{string.Join("|", _header)}|")
						.AppendLine($"|{string.Join("|", Enumerable.Repeat("-", _header.Count))}|");
				}

				foreach(var line in _body)
				{
					builder.AppendLine($"|{string.Join("|", line)}|");
				}

				return builder.ToString();
			}
		}
	}
}
