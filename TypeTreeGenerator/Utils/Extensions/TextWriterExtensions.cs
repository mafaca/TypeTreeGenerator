using System.IO;

namespace TypeTreeGenerator
{
	public static class TextWriterExtensions
	{
		public static TextWriter WriteIndent(this TextWriter _this, int indent)
		{
			for(int i = 0; i < indent; i++)
			{
				_this.Write('\t');
			}
			return _this;
		}
	}
}
