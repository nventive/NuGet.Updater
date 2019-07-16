using System;
using System.IO;
using System.Text;

namespace Nuget.Updater.Entities
{
	public class ActionTextWriter : TextWriter
	{
		private readonly Action<string> _writeAction;

		public ActionTextWriter(Action<string> writeAction)
		{
			_writeAction = writeAction ?? new Action<string>(_ => { });
		}

		public override void Write(string value) => _writeAction(value);

		public override Encoding Encoding => Encoding.Default;
	}
}
