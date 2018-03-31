using System;

namespace TypeTreeGenerator
{
	public enum BaseType
	{
		@bool,
		UInt8,
		SInt16,
		UInt16,
		@int,
		unsignedint,
		SInt64,
		UInt64,
		@float,
	}

	public static class BaseTypeExtensions
	{
		public static string ToExportType(this BaseType _this)
		{
			switch(_this)
			{
				case BaseType.@bool:
					return "bool";
				case BaseType.UInt8:
					return "byte";
				case BaseType.SInt16:
					return "short";
				case BaseType.UInt16:
					return "ushort";
				case BaseType.@int:
					return "int";
				case BaseType.unsignedint:
					return "uint";
				case BaseType.SInt64:
					return "long";
				case BaseType.UInt64:
					return "ulong";
				case BaseType.@float:
					return "float";

				default:
					throw new NotSupportedException(_this.ToString());
			}
		}

		public static string ToExportNETType(this BaseType _this)
		{
			switch (_this)
			{
				case BaseType.@bool:
					return "Boolean";
				case BaseType.UInt8:
					return "Byte";
				case BaseType.SInt16:
					return "Int16";
				case BaseType.UInt16:
					return "UInt16";
				case BaseType.@int:
					return "Int32";
				case BaseType.unsignedint:
					return "UInt32";
				case BaseType.SInt64:
					return "Int64";
				case BaseType.UInt64:
					return "UInt64";
				case BaseType.@float:
					return "Single";

				default:
					throw new NotSupportedException(_this.ToString());
			}
		}
	}
}
