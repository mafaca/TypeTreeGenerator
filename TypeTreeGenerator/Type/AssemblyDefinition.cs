using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;

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
				if(type.IsBasic)
				{
					continue;
				}
				if(type.IsBuiltIn)
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
