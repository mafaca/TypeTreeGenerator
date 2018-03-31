using System.IO;
using System.Text;

namespace TypeTreeGenerator
{
	public class TreeReader : BinaryReader
	{
		public TreeReader(Stream stream):
			base(stream, Encoding.Default, true)
		{
		}

		public int Line { get; set; }
	}
}
