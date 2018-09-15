using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;
using System;

namespace TypeTreeGenerator
{
	public sealed class AssemblyDefinition
	{
		public bool HasType(string name)
		{
			return Types.Any(t => t.Name == name);
		}

		public TypeDefinition FindType(string name)
		{
			return Types.Find(t => t.Name == name);
		}

		public void Export(string path)
		{
			TypeDefinition root = Types[Types.Count - 1];
			string rootName = FixFileName(root.Name);
			string folderPath = Path.Combine(path, rootName);
			if (Directory.Exists(folderPath))
			{
				Directory.Delete(folderPath, true);
				Thread.Sleep(1000);
			}
			Directory.CreateDirectory(folderPath);

			foreach (TypeDefinition type in Types)
			{
				if(IsSkip(type))
				{
					continue;
				}

				string name = FixFileName(type.Name);				
				string filePath = Path.Combine(folderPath, name + ".cs");
				using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.Write))
				{
					using (StreamWriter writer = new StreamWriter(file, Encoding.UTF8))
					{
						type.Export(writer, root);
					}
				}
			}
		}

		private static bool IsSkip(TypeDefinition type)
		{
			if (type.IsBasic)
			{
				return true;
			}
			if(type.IsArray)
			{
				return true;
			}
			if (type.IsVector)
			{
				return true;
			}
			if (type.IsSet)
			{
				return true;
			}
			if (type.IsMap)
			{
				return true;
			}
			if (type.Name.StartsWith("pair", StringComparison.Ordinal))
			{
				return true;
			}
			return false;
		}

		private static string FixFileName(string name)
		{
			int index = name.IndexOf('<');
			if (index != -1)
			{
				name = name.Substring(0, index);
			}
			return name;
		}

		public List<TypeDefinition> Types { get; } = new List<TypeDefinition>();
	}
}
