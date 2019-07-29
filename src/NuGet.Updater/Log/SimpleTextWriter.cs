using System;
using System.IO;
using System.Text;

namespace NuGet.Updater.Log
{
	public class SimpleTextWriter : TextWriter
	{
		private readonly Action<string> _writeAction;

		public SimpleTextWriter(Action<string> writeAction)
		{
			_writeAction = writeAction ?? new Action<string>(_ => { });
		}

		public override void Write(string value) => _writeAction(value);

		public override Encoding Encoding => Encoding.Default;
	}
}
