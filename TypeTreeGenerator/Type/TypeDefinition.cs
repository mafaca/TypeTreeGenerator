using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TypeTreeGenerator
{
	public sealed class TypeDefinition
	{
		public TypeDefinition(string name, string baseName, IReadOnlyList<FieldDefinition> fields)
		{
			Name = FixName(name);
			BaseName = baseName;
			Fields = fields;

			ExportName = ToExportName(Name);
		}

		public void Export(TextWriter writer, TypeDefinition root)
		{
			ExportUsings(writer, root);

			writer.WriteLine(this == root ? "namespace uTinyRipper.Classes" : $"namespace uTinyRipper.Classes.{root.Name}s");
			writer.WriteLine('{');
			if(this == root)
			{
				writer.WriteIndent(1).WriteLine($"public sealed class {Name} : {BaseName}");
				writer.WriteIndent(1).WriteLine('{');
				writer.WriteIndent(2).WriteLine($"public {Name}(AssetInfo assetInfo):");
				writer.WriteIndent(3).WriteLine("base(assetInfo)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(2).WriteLine('}');
				writer.WriteLine();
			}
			else
			{
				writer.WriteIndent(1).WriteLine($"public struct {Name} : IAssetReadable, IYAMLExportable");
				writer.WriteIndent(1).WriteLine('{');
			}

			writer.WriteIndent(2).WriteLine("/*private static int GetSerializedVersion(Version version)");
			writer.WriteIndent(2).WriteLine('{');
			writer.WriteIndent(3).WriteLine("if (Config.IsExportTopmostSerializedVersion)");
			writer.WriteIndent(3).WriteLine('{');
			writer.WriteIndent(4).WriteLine("return 2;");
			writer.WriteIndent(3).WriteLine('}');
			writer.WriteLine();
			writer.WriteIndent(3).WriteLine("if (version.IsGreaterEqual())");
			writer.WriteIndent(3).WriteLine('{');
			writer.WriteIndent(4).WriteLine("return 2;");
			writer.WriteIndent(3).WriteLine('}');
			writer.WriteIndent(3).WriteLine("return 1;");
			writer.WriteIndent(2).WriteLine("}*/");
			writer.WriteLine();

			if(this == root)
			{
				writer.WriteIndent(2).WriteLine("public override void Read(AssetReader reader)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(3).WriteLine("base.Read(reader);");
				writer.WriteLine();
			}
			else
			{
				writer.WriteIndent(2).WriteLine("public void Read(AssetReader reader)");
				writer.WriteIndent(2).WriteLine('{');
			}
			ExportReading(writer);
			writer.WriteIndent(2).WriteLine('}');
			writer.WriteLine();
			
			ExportFetchDependencies(writer, root);
			ExportYAMLExport(writer, root);

			ExportProperties(writer);
			ExportPublicFields(writer);
			ExportPrivateFields(writer);
			
			writer.WriteIndent(1).WriteLine('}');
			writer.WriteLine('}');
		}

		public override string ToString()
		{
			if (Name == null)
			{
				return base.ToString();
			}
			return Name;
		}

		private void ExportUsings(TextWriter writer, TypeDefinition root)
		{
			if (IsUsingGeneric)
			{
				writer.WriteLine("using System.Collections.Generic;");
			}
			writer.WriteLine("using uTinyRipper.AssetExporters;");
			if(this == root)
			{
				writer.WriteLine($"using uTinyRipper.Classes.{root.Name}s;");
			}
			writer.WriteLine("using uTinyRipper.Exporter.YAML;");
			if (IsContainsDependencies)
			{
				writer.WriteLine("using uTinyRipper.SerializedFiles;");
			}
			writer.WriteLine();
		}

		private void ExportReading(TextWriter writer)
		{
			foreach (FieldDefinition field in Fields)
			{
				writer.WriteIndent(3);
				if (field.IsArray)
				{
					writer.Write($"{field.ExportFieldName} = ");
					writer.WriteLine(field.Type.IsBasic ?
						$"reader.Read{ToBasicNETType(field.Type.ExportName)}Array();" :
						$"reader.ReadArray<{field.TypeExportName}>();");
				}
				else if (field.Type.IsMap)
				{
					writer.WriteLine($"{field.ExportFieldName} = new {field.TypeExportName}();");
					writer.WriteIndent(3).WriteLine($"{field.ExportFieldName}.Read(reader);");
				}
				else
				{
					writer.WriteLine(field.Type.IsBasic ?
						$"{field.ExportPropertyName} = reader.Read{ToBasicNETType(field.Type.Name)}();" :
						$"{field.ExportPropertyName}.Read(reader);");
				}

				if(field.IsAlign)
				{
					writer.WriteIndent(3).WriteLine("reader.AlignStream(AlignType.Align4);");
					writer.WriteIndent(3).WriteLine();
				}
			}
		}

		private void ExportFetchDependencies(TextWriter writer, TypeDefinition root)
		{
			if (!IsContainsDependencies)
			{
				return;
			}

			bool isRoot = this == root;
			string over = isRoot ? "override " : string.Empty;
			writer.WriteIndent(2).WriteLine($"public {over}IEnumerable<Object> FetchDependencies(ISerializedFile file, bool isLog = false)");
			writer.WriteIndent(2).WriteLine('{');
			if (isRoot)
			{
				writer.WriteIndent(3).WriteLine("foreach(Object asset in base.FetchDependencies(file, isLog))");
				writer.WriteIndent(3).WriteLine('{');
				writer.WriteIndent(4).WriteLine("yield return asset;");
				writer.WriteIndent(3).WriteLine('}');
				writer.WriteLine();
			}

			foreach (FieldDefinition field in Fields)
			{
				if (field.Type.IsPointer)
				{
					string logFunc = isRoot ? "ToLogString" : $"() => nameof({Name})";
					if (field.IsArray)
					{
						writer.WriteIndent(3).WriteLine($"foreach ({field.TypeExportName} {field.ExportVariableName} in {field.ExportPropertyName})");
						writer.WriteIndent(3).WriteLine('{');
						writer.WriteIndent(4).WriteLine($"yield return {field.ExportVariableName}.FetchDependency(file, isLog, {logFunc}, \"{field.Name}\");");
						writer.WriteIndent(3).WriteLine('}');
					}
					else
					{
						writer.WriteIndent(3).WriteLine($"yield return {field.ExportPropertyName}.FetchDependency(file, isLog, {logFunc}, \"{field.Name}\");");
					}
				}
				else if(field.Type.IsContainsDependencies)
				{
					if (field.IsArray)
					{
						writer.WriteIndent(3).WriteLine($"foreach ({field.TypeExportName} {field.ExportVariableName} in {field.ExportPropertyName})");
						writer.WriteIndent(3).WriteLine('{');
						writer.WriteIndent(4).WriteLine($"foreach (Object asset in {field.ExportVariableName}.FetchDependencies(file, isLog))");
						writer.WriteIndent(4).WriteLine('{');
						writer.WriteIndent(5).WriteLine($"yield return asset;");
						writer.WriteIndent(4).WriteLine('}');
						writer.WriteIndent(3).WriteLine('}');
					}
					else
					{
						writer.WriteIndent(3).WriteLine($"foreach (Object asset in {field.ExportPropertyName}.FetchDependencies(file, isLog))");
						writer.WriteIndent(3).WriteLine('{');
						writer.WriteIndent(4).WriteLine($"yield return asset;");
						writer.WriteIndent(3).WriteLine('}');
					}
				}
			}

			writer.WriteIndent(2).WriteLine("}");
			writer.WriteLine();

		}

		private void ExportYAMLExport(TextWriter writer, TypeDefinition root)
		{
			if(this == root)
			{
				writer.WriteIndent(2).WriteLine("protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(3).WriteLine("YAMLMappingNode node = base.ExportYAMLRoot(container);");
			}
			else
			{
				writer.WriteIndent(2).WriteLine("public YAMLNode ExportYAML(IExportContainer container)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(3).WriteLine("YAMLMappingNode node = new YAMLMappingNode();");
			}
			writer.WriteIndent(3).WriteLine("//node.AddSerializedVersion(GetSerializedVersion(container.Version));");

			foreach (FieldDefinition field in Fields)
			{
				writer.WriteIndent(3).Write($"node.Add(\"{field.Name}\", ");
				if(field.IsArray)
				{
					writer.WriteLine(IsBasicType(field.Type.ExportName) ?
						$"{field.ExportPropertyName}.ExportYAML());" :
						$"{field.ExportPropertyName}.ExportYAML(container));");
				}
				else if(field.Type.IsMap)
				{
					writer.WriteLine(IsConsistOfBasic(field.Type.Name) ?
						$"{field.ExportPropertyName}.ExportYAML());" :
						$"{field.ExportPropertyName}.ExportYAML(container));");
				}
				else
				{
					writer.WriteLine(field.Type.IsBasic ?
						$"{field.ExportPropertyName});" :
						$"{field.ExportPropertyName}.ExportYAML(container));");
				}
			}
			writer.WriteIndent(3).WriteLine("return node;");
			writer.WriteIndent(2).WriteLine('}');
		}

		private void ExportProperties(TextWriter writer)
		{
			bool wrote = false;
			foreach (FieldDefinition field in Fields)
			{
				if(field.IsArray || field.Type.IsBasic || field.Type.IsCollection)
				{
					if (!wrote)
					{
						writer.WriteLine();
						wrote = true;
					}

					if (field.IsArray)
					{
						writer.WriteIndent(2).WriteLine($"public IReadOnlyList<{field.TypeExportName}> {field.ExportPropertyName} => {field.ExportFieldName};");
					}
					else if (field.Type.IsBasic)
					{
						writer.WriteIndent(2).WriteLine($"public {field.TypeExportName} {field.ExportPropertyName} {{ get; private set; }}");
					}
					else
					{
						if(field.Type.IsMap)
						{
							writer.WriteIndent(2).WriteLine($"public IReadOnly{field.TypeExportName} {field.ExportPropertyName} => {field.ExportFieldName};");
						}
						else if(field.Type.IsSet)
						{
							writer.WriteIndent(2).WriteLine($"public IReadOnlyList<{field.TypeExportName}> {field.ExportPropertyName} => {field.ExportFieldName};");
						}
						else
						{
							throw new NotImplementedException();
						}
					}
				}
			}
		}

		private void ExportPublicFields(TextWriter writer)
		{
			bool wrote = false;
			foreach (FieldDefinition field in Fields)
			{
				if (field.Type.IsBasic)
				{
					continue;
				}
				if(field.IsArray)
				{
					continue;
				}
				if(field.Type.IsCollection)
				{
					continue;
				}

				if (!wrote)
				{
					writer.WriteLine();
					wrote = true;
				}
				writer.WriteIndent(2).WriteLine($"public {field.TypeExportName} {field.ExportPropertyName};");
				wrote = true;
			}
		}

		private void ExportPrivateFields(TextWriter writer)
		{
			bool wrote = false;
			foreach (FieldDefinition field in Fields)
			{
				if (field.IsArray)
				{
					if (!wrote)
					{
						writer.WriteLine();
						wrote = true;
					}
					writer.WriteIndent(2).WriteLine($"private {field.TypeExportName}[] {field.ExportFieldName};");
				}
				else if (field.Type.IsMap)
				{
					if (!wrote)
					{
						writer.WriteLine();
						wrote = true;
					}
					writer.WriteIndent(2).WriteLine($"private {field.TypeExportName} {field.ExportFieldName};");
				}
			}
		}

		private static string GetElement(string name, int index)
		{
			int startIndex = name.IndexOf('<') + 1;
			string element = name.Substring(startIndex, name.Length - startIndex - 1);
			string[] elements = element.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			return elements[index];
		}

		private static string FixName(string name)
		{
			if (Enum.TryParse(name, out BasicType type))
			{
				return type.ToExportType();
			}
			if (IsArrayType(name))
			{
				string element = name.Substring(0, name.Length - 2);
				string exportElement = FixName(element);
				return $"{exportElement}[]";
			}
			if (IsVectorType(name))
			{
				int startIndex = name.IndexOf('<') + 1;
				string element = name.Substring(startIndex, name.Length - startIndex - 1);
				string exportElement = FixName(element);
				return $"vector<{exportElement}>";
			}
			if (IsSetType(name))
			{
				int startIndex = name.IndexOf('<') + 1;
				string element = name.Substring(startIndex, name.Length - startIndex - 1);
				string exportElement = FixName(element);
				return $"set<{exportElement}>";
			}
			if (IsMapType(name))
			{
				int startIndex = name.IndexOf('<') + 1;
				string element = name.Substring(startIndex, name.Length - startIndex - 1);
				string[] elements = SplitGeneric(element);
				string exportElement = FixName(elements[0]);
				string exportElement2 = FixName(elements[1]);
				return $"map<{exportElement},{exportElement2}>";
			}
			return name;
		}

		private static string ToExportName(string name)
		{
			if(IsArrayType(name))
			{
				string element = name.Substring(0, name.Length - 2);
				return ToExportName(element);
			}
			if (IsVectorType(name) || IsSetType(name))
			{
				int startIndex = name.IndexOf('<') + 1;
				string element = name.Substring(startIndex, name.Length - startIndex - 1);
				return ToExportName(element);
			}
			if (IsMapType(name))
			{
				int startIndex = name.IndexOf('<') + 1;
				string element = name.Substring(startIndex, name.Length - startIndex - 1);
				string[] elements = SplitGeneric(element);
				string exportElement = ToExportName(elements[0]);
				string exportElement2 = ToExportName(elements[1]);
				return $"Dictionary<{exportElement}, {exportElement2}>";
			}
			return name;
		}

		private static string[] SplitGeneric(string name)
		{
			int index = name.IndexOf(',');
			string left = name.Substring(0, index).Trim();
			string right = name.Substring(index + 1).Trim();
			return new [] { left, right };
		}

		private static bool IsConsistOfBasic(string name)
		{
			if(IsBasicType(name))
			{
				return true;
			}
			if(IsMapType(name))
			{
				string element1 = GetElement(name, 0);
				string element2 = GetElement(name, 1);
				if (IsBasicType(element1) && IsBasicType(element2))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsBasicType(string name)
		{
			switch (name)
			{
				case "bool":
				case "char":
				case "byte":
				case "short":
				case "ushort":
				case "int":
				case "uint":
				case "long":
				case "ulong":
				case "float":
				case "string":
					return true;

				default:
					return false;
			}
		}

		private static string ToBasicNETType(string name)
		{
			switch (name)
			{
				case "bool":
					return "Boolean";
				case "char":
					return "Char";
				case "byte":
					return "Byte";
				case "short":
					return "Int16";
				case "ushort":
					return "UInt16";
				case "int":
					return "Int32";
				case "uint":
					return "UInt32";
				case "long":
					return "Int64";
				case "ulong":
					return "UInt64";
				case "float":
					return "Single";
				case "string":
					return "String";

				default:
					throw new NotSupportedException(name);
			}
		}

		private static bool IsArrayType(string name)
		{
			return name.EndsWith("[]", StringComparison.Ordinal);
		}

		private static bool IsVectorType(string name)
		{
			return name.StartsWith("vector<", StringComparison.Ordinal);
		}

		private static bool IsSetType(string name)
		{
			return name.StartsWith("set<", StringComparison.Ordinal);
		}

		private static bool IsMapType(string name)
		{
			return name.StartsWith("map<", StringComparison.Ordinal);
		}

		public string Name { get; }
		public string BaseName { get; }
		public IReadOnlyList<FieldDefinition> Fields { get; }

		public string ExportName { get; }

		public bool IsBasic => IsBasicType(ExportName);
		public bool IsArray => IsArrayType(Name);
		public bool IsPointer => ExportName.StartsWith("PPtr<", StringComparison.Ordinal);
		public bool IsVector => IsVectorType(Name);
		public bool IsSet => IsSetType(Name);
		public bool IsMap => IsMapType(Name);

		private bool IsContainsDependencies => Fields.Any(t => t.Type.IsPointer || t.Type.IsContainsDependencies);
		private bool IsCollection => IsVector || IsSet || IsMap;

		private bool IsUsingGeneric
		{
			get
			{
				foreach(FieldDefinition field in Fields)
				{
					if (field.IsArray)
					{
						return true;
					}
					if (field.Type.IsMap)
					{
						return true;
					}
				}
				return IsContainsDependencies;
			}
		}
	}
}
