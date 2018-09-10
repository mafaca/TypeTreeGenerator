using System;
using System.IO;

namespace TypeTreeGenerator
{
	public class Program
	{
		static void Main(string[] args)
		{
			using (FileStream stream = new FileStream("../../Test/tree.txt", FileMode.Open, FileAccess.Read))
			{
				using (TreeReader reader = new TreeReader(stream))
				{
					using (TreeParser parser = new TreeParser(reader))
					{
						parser.Parse();

						AssemblyDefinition assembly = parser.GenerateAssembly();
						string exportPath = "Export";
						if(!Directory.Exists(exportPath))
						{
							Directory.CreateDirectory(exportPath);
						}

						assembly.Export(exportPath);
					}
				}
			}

			Console.WriteLine("Finished");
			Console.ReadKey();
		}
	}
}
