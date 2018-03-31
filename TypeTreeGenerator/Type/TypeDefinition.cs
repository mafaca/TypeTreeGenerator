using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

			if(this == root)
			{
				writer.WriteLine($"namespace UtinyRipper.Classes");
			}
			else
			{
				writer.WriteLine($"namespace UtinyRipper.Classes.{root.Name}s");
			}
			writer.WriteLine('{');
			if(this == root)
			{
				if (BaseName == string.Empty)
				{
					writer.WriteIndent(1).WriteLine($"public sealed class {Name}");
				}
				else
				{
					writer.WriteIndent(1).WriteLine($"public sealed class {Name} : {BaseName}");
				}
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
			
			ExportYAMLExport(writer, root);

			ExportProperties(writer);
			ExportPublicFields(writer);
			ExportPrivateFields(writer);
			
			writer.WriteIndent(1).WriteLine('}');
			writer.WriteLine('}');
		}

		private void ExportUsings(TextWriter writer, TypeDefinition root)
		{
			if (Fields.Any(t => t.IsArray))
			{
				writer.WriteLine($"using System.Collections.Generic;");
			}
			writer.WriteLine($"using UtinyRipper.AssetExporters;");
			if(this == root)
			{
				writer.WriteLine($"using UtinyRipper.Classes.{root.Name}s;");
			}
			writer.WriteLine($"using UtinyRipper.Exporter.YAML;");
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
					if(field.Type.IsBasic)
					{
						writer.WriteLine($"stream.Read{field.Type.ToBaseType.ToExportNETType()}Array();");
					}
					else
					{
						writer.WriteLine($"stream.ReadArray<{field.TypeExportName}>();");
					}
				}
				else
				{
					if (field.Type.IsBasic)
					{
						writer.WriteLine($"{field.ExportPropertyName} = stream.Read{field.Type.ToBaseType.ToExportNETType()}();");
					}
					else
					{
						writer.WriteLine($"{field.ExportPropertyName}.Read(stream);");
					}
				}

				if(field.IsAlign)
				{
					writer.WriteIndent(3).WriteLine($"stream.AlignStream(AlignType.Align4);");
					writer.WriteIndent(3).WriteLine();
				}
			}
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
					if (field.Type.IsBasic)
					{
						writer.WriteLine($"{field.ExportPropertyName});");
					}
					else
					{
						writer.WriteLine($"{field.ExportPropertyName}.ExportYAML(exporter));");
					}
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
				if (field.IsArray)
				{
					writer.WriteIndent(2).WriteLine($"public IReadOnlyList<{field.TypeExportName}> {field.ExportPropertyName} => {field.ExportFieldName};");
				}
				else
				{
					writer.WriteIndent(2).WriteLine($"public {field.TypeExportName} {field.ExportPropertyName} {{ get; private set; }}");
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

		private string GetTypeName(string name)
		{
			if(Enum.TryParse(name, out BaseType type))
			{
				return type.ToExportType();
			}
			return name;
		}

		public bool IsBasic => Enum.TryParse(Name, out BaseType type);

		public string BaseName { get; set; }
		public string Name { get; set; }
		public List<FieldDefinition> Fields { get; } = new List<FieldDefinition>();
		public bool IsInner { get; set; }

		public string ExportName => GetTypeName(Name);
		public BaseType ToBaseType => (BaseType)Enum.Parse(typeof(BaseType), Name);
	}
}
