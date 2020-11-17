using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoGet.Extensions;
using Uno.Extensions;

namespace NeoGet.Helpers
{
	public static class MarkdownHelper
	{
		internal static string Link(string text, string url) => url.HasValue() ? $"[{text}]({url})" : text;

		internal static string Bold(string text) => $"**{text}**";

		internal static string CodeBlocksEnumeration(IEnumerable<string> values) => values.Select(v => CodeBlock(v)).GetEnumeration();

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

		internal static string Table(
			string[] header,
			string[][] rows,
			bool prettify
		) => new TableBuilder()
			.AddHeader(header)
			.AddRows(rows)
			.Build(prettify);

		private class TableBuilder
		{
			private readonly List<string> _header = new List<string>();
			private readonly List<string[]> _body = new List<string[]>();

			public TableBuilder AddHeader(params string[] header)
				=> this.Apply(b => b._header.AddRange(header));

			public TableBuilder AddRows(params string[][] rows)
				=> this.Apply(b => _body.AddRange(rows));

			public string Build(bool prettify)
			{
				var builder = new StringBuilder();

				if(_header.Any())
				{
					builder
						.AppendLine(GetTableRow(_header, prettify))
						.AppendLine(GetTableRow(_header.Select(_ => "-"), prettify));
				}

				foreach(var row in _body)
				{
					builder.AppendLine(GetTableRow(row, prettify));
				}

				return builder.ToString();
			}

			private string GetTableRow(IEnumerable<string> row, bool prettify)
				=> "|"
					+ row.Select((c, index) => GetTableCellContent(c, index, prettify)).JoinBy("|")
					+ "|";

			private string GetTableCellContent(string content, int columnIndex, bool prettify)
			{
				var columnLength = prettify ? GetColumnLength(columnIndex) + 2 : 0;

				if(columnLength <= content.Length)
				{
					return content;
				}
				else if(content == "-")
				{
					return string.Join("", Enumerable.Repeat(content, columnLength));
				}
				else
				{
					return content
						.PadLeft(content.Length + 1)
						.PadRight(columnLength);
				}
			}

			private int GetColumnLength(int index)
				=> _body
					.Select(l => l.ElementAtOrDefault(index))
					.Concat(_header.ElementAtOrDefault(index))
					.Trim()
					.Max(s => s.Length);
		}
}
}
