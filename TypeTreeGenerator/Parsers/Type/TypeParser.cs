using System;
using System.Collections.Generic;

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

			VarName = FindReadWord();
			if(VarName == string.Empty)
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
					TypeName = nameof(BaseType.unsignedint);
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
						throw CreateException($"Invalid indent");
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
						throw CreateException($"Invlid intent");
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

		public TypeDefinition GenerateType(AssemblyDefinition assembly)
		{
			string typeName = GetTypeDefinitionName();
			TypeDefinition type = assembly.FindType(typeName);
			if(type == null)
			{
				type = new TypeDefinition();
				type.Name = typeName;
				foreach (TypeParser child in Children)
				{
					FieldDefinition field = GenerateField(assembly, child);
					type.Fields.Add(field);
				}
				type.IsInner = GetIsInner();

				assembly.Types.Add(type);
			}
			return type;
		}

		private static FieldDefinition GenerateField(AssemblyDefinition assembly, TypeParser child)
		{
			if(child.TypeName == "Array")
			{
				return GenerateField(assembly, child.Children[1]);
			}

			TypeDefinition childType = child.GenerateType(assembly);
			FieldDefinition field = new FieldDefinition();
			field.Name = child.VarName;
			field.Type = childType;
			field.IsArray = child.TypeName == "vector";
			field.IsAlign = child.MetaFlag.IsAlign();
			return field;
		}

		private string GetTypeDefinitionName()
		{
			if(TypeName == "vector")
			{
				TypeParser arrayType = GetArrayType();
				return $"{arrayType.GetTypeDefinitionName()}[]";
			}
			if(TypeName == "map")
			{
				TypeParser pair = GetArrayType();
				if(pair.TypeName != "pair")
				{
					throw new Exception($"Pair has unsupported type {pair.TypeName}");
				}
				if (pair.Children.Count != 2)
				{
					throw new Exception($"Pair contains {pair.Children.Count} children");
				}

				TypeParser first = pair.Children[0];
				if(first.VarName != "first")
				{
					throw new Exception($"First has unsupported name {first.VarName}");
				}
				TypeParser second = pair.Children[1];
				if (second.VarName != "second")
				{
					throw new Exception($"Second has unsupported name {second.VarName}");
				}
				return $"map<{first.GetTypeDefinitionName()}, {second.GetTypeDefinitionName()}>";
			}
			if(TypeName == "pair")
			{
				if (VarName != "data")
				{
					throw new Exception($"Pair has unsupported type {VarName}");
				}
				if (Children.Count != 2)
				{
					throw new Exception($"Pair contains {Children.Count} children");
				}
				TypeParser first = Children[0];
				if (first.VarName != "first")
				{
					throw new Exception($"First has unsupported name {first.VarName}");
				}
				TypeParser second = Children[1];
				if (second.VarName != "second")
				{
					throw new Exception($"Second has unsupported name {second.VarName}");
				}
				return $"pair<{first.GetTypeDefinitionName()}, {second.GetTypeDefinitionName()}>";
			}

			return TypeName;
		}

		private TypeParser GetArrayType()
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
				throw new Exception($"Array has unsupported type {array.TypeName}");
			}
			if (array.VarName != "Array")
			{
				throw new Exception($"Array has unsupported name {array.VarName}");
			}

			TypeParser size = array.Children[0];
			if(size.TypeName != nameof(BaseType.@int))
			{
				throw new Exception($"Size has unsupported type {size.TypeName}");
			}
			if (size.VarName != "size")
			{
				throw new Exception($"Size has unsupported name {size.VarName}");
			}

			TypeParser data = array.Children[1];
			if (data.VarName != "data")
			{
				throw new Exception($"Data has unsupported name {data.VarName}");
			}
			return data;
		}

		private bool GetIsInner()
		{
			if(TypeName == "vector")
			{
				return true;
			}
			if(TypeName == "map")
			{
				return true;
			}
			if(TypeName == "pair")
			{
				return true;
			}
			return false;
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
