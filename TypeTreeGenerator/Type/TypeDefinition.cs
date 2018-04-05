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
			writer.WriteLine("#warning TODO: serialized version acording to read version (current 2017.3.0f3)");
			writer.WriteIndent(3).WriteLine("return 2;");
			writer.WriteIndent(2).WriteLine("}*/");
			writer.WriteLine();

			if(this == root)
			{
				writer.WriteIndent(2).WriteLine("public override void Read(AssetStream stream)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(3).WriteLine("base.Read(stream);");
				writer.WriteLine();
			}
			else
			{
				writer.WriteIndent(2).WriteLine("public void Read(AssetStream stream)");
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

		private void ExportUsings(TextWriter writer, TypeDefinition root)
		{
			if (Fields.Any(t => t.IsArray) || IsContainsDependencies)
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
						$"stream.Read{field.Type.ToBaseType.ToExportNETType()}Array();" :
						$"stream.ReadArray<{field.TypeExportName}>();");
				}
				else
				{
					writer.WriteLine(field.Type.IsBasic ?
						$"{field.ExportPropertyName} = stream.Read{field.Type.ToBaseType.ToExportNETType()}();" :
						$"{field.ExportPropertyName}.Read(stream);");
				}

				if(field.IsAlign)
				{
					writer.WriteIndent(3).WriteLine("stream.AlignStream(AlignType.Align4);");
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
				writer.WriteIndent(3).WriteLine("foreach(Object @object in base.FetchDependencies(file, isLog))");
				writer.WriteIndent(3).WriteLine("{");
				writer.WriteIndent(4).WriteLine("yield return @object;");
				writer.WriteIndent(3).WriteLine("}");
				writer.WriteLine();
			}

			foreach (FieldDefinition field in Fields)
			{
				if (!field.Type.IsPointer)
				{
					continue;
				}

				string logFunc = isRoot ? "ToLogString" : $"() => nameof({Name})";
				writer.WriteIndent(3).WriteLine($"yield return {field.ExportPropertyName}.FetchDependency(file, isLog, {logFunc}, \"{field.Name}\");");
			}

			writer.WriteIndent(2).WriteLine("}");
			writer.WriteLine();

		}

		private void ExportYAMLExport(TextWriter writer, TypeDefinition root)
		{
			if(this == root)
			{
				writer.WriteIndent(2).WriteLine("protected override YAMLMappingNode ExportYAMLRoot(IAssetsExporter exporter)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(3).WriteLine("YAMLMappingNode node = base.ExportYAMLRoot(exporter);");
			}
			else
			{
				writer.WriteIndent(2).WriteLine("public YAMLNode ExportYAML(IAssetsExporter exporter)");
				writer.WriteIndent(2).WriteLine('{');
				writer.WriteIndent(3).WriteLine("YAMLMappingNode node = new YAMLMappingNode();");
			}
			writer.WriteIndent(3).WriteLine("//node.AddSerializedVersion(GetSerializedVersion(exporter.Version));");

			foreach (FieldDefinition field in Fields)
			{
				writer.WriteIndent(3).Write($"node.Add(\"{field.Name}\", ");
				if (field.IsArray)
				{
					writer.WriteLine($"{field.ExportPropertyName}.ExportYAML(exporter));");
				}
				else
				{
					writer.WriteLine(field.Type.IsBasic ?
						$"{field.ExportPropertyName});" :
						$"{field.ExportPropertyName}.ExportYAML(exporter));");
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
				if (!field.Type.IsBasic)
				{
					if (!field.IsArray)
					{
						continue;
					}
				}

				if (!wrote)
				{
					writer.WriteLine();
					wrote = true;
				}

				writer.WriteIndent(2).WriteLine(field.IsArray ?
					$"public IReadOnlyList<{field.TypeExportName}> {field.ExportPropertyName} => {field.ExportFieldName};" :
					$"public {field.TypeExportName} {field.ExportPropertyName} {{ get; private set; }}");
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
				if (!field.IsArray)
				{
					continue;
				}

				if (!wrote)
				{
					writer.WriteLine();
					wrote = true;
				}
				writer.WriteIndent(2).WriteLine($"private {field.TypeExportName}[] {field.ExportFieldName};");
			}
		}

		public bool IsBasic => Enum.TryParse(TypeName, out BaseType _);

		public string BaseName { get; set; }
		public string Name { get; set; }
		public List<FieldDefinition> Fields { get; } = new List<FieldDefinition>();
		public bool IsInner { get; set; }

		public string ExportName => ExportTypeName;
		public BaseType ToBaseType => (BaseType)Enum.Parse(typeof(BaseType), Name);

		private string TypeName => /*IsArray ? Fields[0].Type.Name : */Name;
		private string ExportTypeName
		{
			get
			{
				if(Enum.TryParse(TypeName, out BaseType type))
				{
					//if (IsArray)
					{
						//return $"{type.ToExportType()}[]";
					}
					//else
					{
						return type.ToExportType();
					}
				}
				return Name;
			}
		}

		//private bool IsArray => s_arrayRegex.IsMatch(Name);
		private bool IsPointer => s_pointeRegex.IsMatch(Name);
		private bool IsContainsDependencies => Fields.Any(t => t.Type.IsPointer || t.Type.IsContainsDependencies);
		
		//private static readonly Regex s_arrayRegex = new Regex(@"[\w\<\>_]\[\]*>");
		private static readonly Regex s_pointeRegex = new Regex(@"PPtr<[\w\<\>_]*>");
	}
}
