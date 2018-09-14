using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TypeTreeGenerator
{
	public sealed class TypeDefinition
	{
		public override string ToString()
		{
			if(Name == null)
			{
				return base.ToString();
			}
			return Name;
		}

		public void Export(TextWriter writer, TypeDefinition root)
		{
			ExportUsings(writer, root);

			writer.WriteLine(this == root ? "namespace UtinyRipper.Classes" : $"namespace UtinyRipper.Classes.{root.Name}s");
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

		private static bool IsBasicType(string name)
		{
			return Enum.TryParse(name, out BaseType _);
		}

		private static BaseType ToBasicType(string name)
		{
			return (BaseType)Enum.Parse(typeof(BaseType), name); ;
		}


		private void ExportUsings(TextWriter writer, TypeDefinition root)
		{
			if (IsUsingGeneric)
			{
				writer.WriteLine("using System.Collections.Generic;");
			}
			writer.WriteLine("using UtinyRipper.AssetExporters;");
			if(this == root)
			{
				writer.WriteLine($"using UtinyRipper.Classes.{root.Name}s;");
			}
			writer.WriteLine("using UtinyRipper.Exporter.YAML;");
			if (IsContainsDependencies)
			{
				writer.WriteLine("using UtinyRipper.SerializedFiles;");
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
						$"reader.Read{field.Type.ToBasic.ToExportNETType()}Array();" :
						$"reader.ReadArray<{field.TypeExportName}>();");
				}
				else if (field.Type.IsMap)
				{
					writer.WriteLine($"{field.ExportFieldName} = new {field.TypeExportName}();");
					writer.WriteIndent(3).WriteLine($"{field.ExportFieldName}.Read(reader);");
				}
				else if (field.Type.IsSet)
				{
					writer.Write($"{field.ExportFieldName} = ");
					writer.WriteLine(IsBasicType(field.TypeExportName) ?
					 $"reader.Read{ToBasicType(field.TypeExportName).ToExportNETType()}Array();" :
					 $"reader.ReadArray<{field.TypeExportName}>();");
				}
				else
				{
					writer.WriteLine(field.Type.IsBasic ?
						$"{field.ExportPropertyName} = reader.Read{field.Type.ToBasic.ToExportNETType()}();" :
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
				if (!field.Type.IsPointer)
				{
					continue;
				}

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

			writer.WriteIndent(2).WriteLine("}");
			writer.WriteLine();

		}

		private void ExportYAMLExport(TextWriter writer, TypeDefinition root)
		{
			if(this == root)
			{
				writer.WriteIndent(2).WriteLine("protected override YAMLMappingNode ExportYAMLRoot(IExportContainer container)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteLine("#warning TODO: values acording to read version (current 2017.3.0f3)");
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
				if (field.IsArray)
				{
					writer.WriteLine($"{field.ExportPropertyName}.ExportYAML(container));");
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
				else if(field.Type.IsSet)
				{
					if (!wrote)
					{
						writer.WriteLine();
						wrote = true;
					}
					writer.WriteIndent(2).WriteLine($"private {field.TypeExportName}[] {field.ExportFieldName};");
				}
			}
		}

		private string ToExportName(string name)
		{
			if (Enum.TryParse(name, out BaseType type))
			{
				return type.ToExportType();
			}
			if (IsMap)
			{
				return name.Replace("map<", "Dictionary<");
			}
			if (IsSet)
			{
				return name.Substring(4, name.Length - 5);
			}
			return name;
		}

		public bool IsBasic => IsBasicType(Name);

		public string BaseName { get; set; }
		public string Name { get; set; }
		public List<FieldDefinition> Fields { get; } = new List<FieldDefinition>();
		public bool IsBuiltIn { get; set; }

		public string ExportName => ToExportName(Name);
		public BaseType ToBasic => ToBasicType(Name);

		private bool IsPointer => s_pointerRegex.IsMatch(Name);
		private bool IsContainsDependencies => Fields.Any(t => t.Type.IsPointer || t.Type.IsContainsDependencies);
		private bool IsCollection => IsMap || IsSet;
		private bool IsMap => Name.StartsWith("map<", StringComparison.Ordinal);
		private bool IsSet => Name.StartsWith("set<", StringComparison.Ordinal);

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
					if (field.Type.IsCollection)
					{
						return true;
					}
				}
				return IsContainsDependencies;
			}
		}
		
		private static readonly Regex s_pointerRegex = new Regex(@"PPtr<[\w\<\>_]*>");
	}
}
