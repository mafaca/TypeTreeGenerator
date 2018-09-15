using System.Collections.Generic;

namespace TypeTreeGenerator
{
	public class TreeParser : BaseParser
	{
		public TreeParser(TreeReader reader):
			base(reader)
		{
			m_header = new TreeHeaderParser(reader);
			m_type = new TypeParser(reader, 0);
		}

		public void Parse()
		{
			m_header.Parse();
			bool found = FindNextLine();
			if(!found)
			{
				throw CreateException("Type wasn't found");
			}

			m_type.Parse();
		}

		public AssemblyDefinition GenerateAssembly()
		{
			AssemblyDefinition assembly = new AssemblyDefinition();
			m_type.GenerateType(assembly, m_header.BaseName);
			return assembly;
		}
		
		private readonly TreeHeaderParser m_header;
		private readonly TypeParser m_type;
	}
}
