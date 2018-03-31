using System.Collections.Generic;

namespace TypeTreeGenerator
{
	public class TypeParserComparer : IEqualityComparer<TypeParser>
	{
		public bool Equals(TypeParser x, TypeParser y)
		{
			if (y == null)
			{
				return false;
			}
			return x.TypeName == y.TypeName;
		}

		public int GetHashCode(TypeParser obj)
		{
			return unchecked(17 + 23 * obj.TypeName.GetHashCode());
		}

		public static TypeParserComparer Instance { get; } = new TypeParserComparer();
	}
}
