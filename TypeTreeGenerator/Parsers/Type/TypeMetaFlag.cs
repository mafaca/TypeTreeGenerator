using System;

namespace TypeTreeGenerator
{
	[Flags]
	public enum TypeMetaFlag
	{
		Align			= 0x4000,
	}

	public static class TypeMetaFlagExtensions
	{
		public static bool IsAlign(this TypeMetaFlag _this)
		{
			return (_this & TypeMetaFlag.Align) != 0;
		}
	}
}
