using System.IO;

namespace TypeTreeGenerator
{
	internal static class BinaryReaderExtensions
	{
		public static bool EndOfStream(this BinaryReader _this)
		{
			return _this.BaseStream.Position == _this.BaseStream.Length;
		}
	}
}
