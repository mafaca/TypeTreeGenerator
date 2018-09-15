namespace TypeTreeGenerator
{
	public sealed class FieldDefinition
	{
		public FieldDefinition(TypeDefinition type, string name, bool isAlign)
		{
			Type = type;
			Name = name;
			IsAlign = isAlign;
		}

		public override string ToString()
		{
			if(Type == null)
			{
				return base.ToString();
			}
			return $"{Type} {Name}";
		}

		private static string GetVariableName(string name)
		{
			name = name.Replace(" ", string.Empty);
			if (name.StartsWith("m_"))
			{
				name = name.Substring(2);
			}

			if (!char.IsLower(name[0]))
			{
				char firstLetter = char.ToLower(name[0]);
				string part = name.Substring(1);
				name = $"{firstLetter}{part}";
			}
			if (name[name.Length - 1] == 's')
			{
				name = name.Substring(0, name.Length - 1);
			}

			return name;
		}

		private static string GetPropertyName(string name)
		{
			name = name.Replace(" ", string.Empty);
			if (name.StartsWith("m_"))
			{
				name = name.Substring(2);
			}

			if (char.IsUpper(name[0]))
			{
				return name;
			}
			else
			{
				char firstLetter = char.ToUpper(name[0]);
				string part = name.Substring(1);
				return $"{firstLetter}{part}";
			}
		}

		private static string GetFieldName(string name)
		{
			name = name.Replace(" ", string.Empty);
			if (char.IsUpper(name[0]))
			{
				char firstLetter = char.ToLower(name[0]);
				string part = name.Substring(1);
				return $"m_{firstLetter}{part}";
			}

			if (name.StartsWith("m_"))
			{
				if (char.IsLower(name[2]))
				{
					return name;
				}

				char firstLetter = char.ToLower(name[2]);
				string part = name.Substring(3);
				return $"m_{firstLetter}{part}";
			}

			return $"m_{name}";
		}

		public TypeDefinition Type { get; set; }
		public string Name { get; set; }
		public bool IsAlign { get; set; }

		public bool IsArray => Type.IsArray || Type.IsVector || Type.IsSet;
		public string TypeExportName => Type.ExportName;
		public string ExportVariableName => GetVariableName(Name);
		public string ExportFieldName => GetFieldName(Name);
		public string ExportPropertyName => GetPropertyName(Name);
	}
}
