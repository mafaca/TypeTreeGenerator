using System;

namespace TypeTreeGenerator
{
	public enum BasicType
	{
		@bool,
		@char,
		UInt8,
		SInt16,
		UInt16,
		@int,
		unsignedint,
		SInt64,
		UInt64,
		@float,
		@string,
	}

	public static class BaseTypeExtensions
	{
		public static string ToExportType(this BasicType _this)
		{
			switch(_this)
			{
				case BasicType.@bool:
					return "bool";
				case BasicType.@char:
					return "char";
				case BasicType.UInt8:
					return "byte";
				case BasicType.SInt16:
					return "short";
				case BasicType.UInt16:
					return "ushort";
				case BasicType.@int:
					return "int";
				case BasicType.unsignedint:
					return "uint";
				case BasicType.SInt64:
					return "long";
				case BasicType.UInt64:
					return "ulong";
				case BasicType.@float:
					return "float";
				case BasicType.@string:
					return "string";

				default:
					throw new NotSupportedException(_this.ToString());
			}
		}
	}
}
