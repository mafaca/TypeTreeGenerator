using System.Collections.Generic;

namespace TypeTreeGenerator
{
	public class TreeHeaderParser : BaseParser
	{
		public TreeHeaderParser(TreeReader reader):
			base(reader)
		{
		}

		public void Parse()
		{
			VarParser typeReader = new VarParser(m_reader, false);

			FindValidateWord("//");
			typeReader.Parse("classID");
			TypeID = typeReader.Value;
			FindValidateSymbol(':');
			
			string typeName = FindReadWord();
			AddType(typeName);

			while (true)
			{
				string arrow = FindReadWord();
				if(arrow == string.Empty)
				{
					break;
				}

				if(arrow != "<-")
				{
					throw CreateException($"Can't find arrow word but found {arrow}");
				}

				typeName = FindReadWord();
				AddType(typeName);
			}
		}

		private void AddType(string type)
		{
			if (type == string.Empty)
			{
				throw CreateException("Can't find type name");
			}
			if (m_inhTypes.Contains(type))
			{
				throw CreateException($"Tree already has type '{type}'");
			}
			m_inhTypes.Add(type);
		}
		
		public int TypeID { get; private set; }
		public string Name => m_inhTypes[0];
		public string BaseName => m_inhTypes.Count > 1 ? m_inhTypes[1] : string.Empty;

		private readonly List<string> m_inhTypes = new List<string>();
	}
}
