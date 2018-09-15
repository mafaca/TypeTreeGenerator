using System;
using System.Collections.Generic;
using System.Text;

namespace TypeTreeGenerator
{
	public sealed class TypeParser : BaseParser
	{
		public TypeParser(TreeReader reader, int indent):
			base(reader)
		{
			m_indent = indent;
		}

		public void Parse()
		{
			VarParser vreader = new VarParser(m_reader, true);

			Validate();

			ReadType();

			VarName = string.Empty;
			FindWord();
			StringBuilder sb = new StringBuilder();
			while (true)
			{
				char symb = (char)m_reader.PeekChar();
				if(symb == '/')
				{
					while(sb[sb.Length - 1] == ' ')
					{
						sb.Length--;
					}
					break;
				}
				sb.Append(m_reader.ReadChar());
			}
			VarName = sb.ToString();
			if (VarName == string.Empty)
			{
				throw CreateException("Can't find variable name");
			}
			FindValidateWord("//");

			vreader.Parse("ByteSize");
			Size = vreader.Value;

			FindValidateSymbol(',');

			vreader.Parse("Index");
			Index = vreader.Value;

			FindValidateSymbol(',');

			vreader.Parse("IsArray");
			IsArray = vreader.Value != 0;

			FindValidateSymbol(',');

			vreader.Parse("MetaFlag");
			MetaFlag = (TypeMetaFlag)vreader.Value;
			
			int childIndent = m_indent + 1;
			while (true)
			{
				int indent = CheckIndent();
				if(indent < childIndent)
				{
					return;
				}
				if(indent > childIndent)
				{
					throw CreateException($"Unsupported indent {indent}");
				}

				FindNextLine();
				TypeParser child = new TypeParser(m_reader, childIndent);
				child.Parse();
				m_children.Add(child);
			}
		}

		public override string ToString()
		{
			if(VarName == null)
			{
				return base.ToString();
			}
			return $"{TypeName} {VarName}";
		}

		private void ReadType()
		{
			TypeName = FindReadWord();
			if (TypeName == string.Empty)
			{
				throw CreateException("Can't find type name");
			}
			if (TypeName == "unsigned")
			{
				long position = m_reader.BaseStream.Position;
				string subtype = FindReadWord();
				if(subtype == "int")
				{
					TypeName = nameof(BasicType.unsignedint);
				}
				else
				{
					m_reader.BaseStream.Position = position;
				}
			}
		}

		private void Validate()
		{
			for (int i = 0; i < m_indent; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					char space = m_reader.ReadChar();
					if (space != ' ')
					{
						throw CreateException("Invalid indent");
					}
				}
			}

			char c = (char)m_reader.PeekChar();
			if (c == ' ')
			{
				throw CreateException($"Indend {m_indent} doesn't match");
			}
		}

		private int CheckIndent()
		{
			long position = m_reader.BaseStream.Position;
			bool isFound = FindNextLine(false);
			if(!isFound)
			{
				return -1;
			}

			for (int i = 0; i < int.MaxValue; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					char space = m_reader.ReadChar();
					if (space != ' ')
					{
						throw CreateException("Invlid intent");
					}
				}

				char c = (char)m_reader.PeekChar();
				if(c != ' ')
				{
					m_reader.BaseStream.Position = position;
					return i + 1;
				}
			}
			throw new NotSupportedException();
		}

		public TypeDefinition GenerateType(AssemblyDefinition assembly, string baseName)
		{
			string name = GetTypeDefinitionName();
			TypeDefinition type = assembly.FindType(name);
			if (type == null)
			{
				FieldDefinition[] fields = new FieldDefinition[Children.Count];
				for (int i = 0; i < Children.Count; i++)
				{
					TypeParser child = Children[i];
					FieldDefinition field = GenerateField(assembly, child);
					fields[i] = field;
				}
				TypeDefinition newType = new TypeDefinition(name, baseName, fields);
				assembly.Types.Add(newType);
				return newType;
			}
			else
			{
				return type;
			}
		}

		private TypeDefinition GenerateType(AssemblyDefinition assembly)
		{
			return GenerateType(assembly, null);
		}

		private static FieldDefinition GenerateField(AssemblyDefinition assembly, TypeParser type)
		{
			TypeDefinition fieldType = type.GenerateType(assembly);
			string fieldName = type.IsArray ? fieldType.Fields[1].Name : type.VarName;
			bool fieldAlign = type.MetaFlag.IsAlign();
			return new FieldDefinition(fieldType, fieldName, fieldAlign);
		}

		private string GetTypeDefinitionName()
		{
			switch (TypeName)
			{
				case "Array":
				{
					TypeParser arrayType = GetArrayElement();
					return $"{arrayType.GetTypeDefinitionName()}[]";
				}
				case "vector":
				{
					TypeParser vectorType = GetElement();
					return $"vector<{vectorType.GetTypeDefinitionName()}>";
				}
				case "set":
				{
					TypeParser setType = GetElement();
					return $"set<{setType.GetTypeDefinitionName()}>";
				}
				case "map":
				{
					TypeParser pair = GetElement();
					if(pair.TypeName != "pair")
					{
						throw new Exception($"Pair has unsupported type {pair.TypeName}");
					}
					if (pair.Children.Count != 2)
					{
						throw new Exception($"Pair contains {pair.Children.Count} children");
					}

					TypeParser key = pair.Children[0];
					if(key.VarName != "first")
					{
						throw new Exception($"First has unsupported name {key.VarName}");
					}
					TypeParser value = pair.Children[1];
					if (value.VarName != "second")
					{
						throw new Exception($"Second has unsupported name {value.VarName}");
					}
					return $"map<{key.GetTypeDefinitionName()}, {value.GetTypeDefinitionName()}>";
				}
				case "pair":
				{
					if (VarName != "data")
					{
						throw new Exception($"Pair has unsupported type {VarName}");
					}
					if (Children.Count != 2)
					{
						throw new Exception($"Pair contains {Children.Count} children");
					}
					TypeParser key = Children[0];
					if (key.VarName != "first")
					{
						throw new Exception($"First has unsupported name {key.VarName}");
					}
					TypeParser value = Children[1];
					if (value.VarName != "second")
					{
						throw new Exception($"Second has unsupported name {value.VarName}");
					}
					return $"pair<{key.GetTypeDefinitionName()}, {value.GetTypeDefinitionName()}>";
				}
				default:
					return TypeName;
			}
		}

		private TypeParser GetElement()
		{
			if (Children.Count != 1)
			{
				throw new Exception($"Root contains {Children.Count} children");
			}
			TypeParser array = Children[0];
			if (array.Children.Count != 2)
			{
				throw new Exception($"Array contains {Children.Count} children");
			}
			if (array.TypeName != "Array")
			{
				throw new Exception($"Unsupported array's type {array.TypeName}");
			}
			if (array.VarName != "Array")
			{
				throw new Exception($"Unsupported array's name {array.VarName}");
			}

			return GetArrayElement(array);
		}

		private TypeParser GetArrayElement()
		{
			return GetArrayElement(this);
		}

		private TypeParser GetArrayElement(TypeParser array)
		{
			TypeParser size = array.Children[0];
			if (size.TypeName != nameof(BasicType.@int))
			{
				throw new Exception($"Unsupported size's type {size.TypeName}");
			}
			if (size.VarName != "size")
			{
				throw new Exception($"Unsupported size's name {size.VarName}");
			}

			TypeParser data = array.Children[1];
			if (data.VarName != "data")
			{
				throw new Exception($"Unsupported data's name {data.VarName}");
			}
			return data;
		}

		public string TypeName { get; private set; }
		public string VarName { get; private set; }
		public int Size { get; private set; }
		public int Index { get; private set; }
		public bool IsArray { get; private set; }
		public TypeMetaFlag MetaFlag { get; private set; }
		public IReadOnlyList<TypeParser> Children => m_children;

		private readonly List<TypeParser> m_children = new List<TypeParser>();

		private readonly int m_indent;
	}
}
