using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTestStub
{
	public class Utils
	{
		public static string[] ReadFile(string fileFullPath)
		{
			return System.IO.File.ReadAllLines(fileFullPath);
		}

		public static string WriteFile(string fileFullPath, string content, bool append = false)
		{
			FileInfo fi = new FileInfo(fileFullPath);
			try
			{
				if (!fi.Exists)
				{
					using (StreamWriter sw = File.CreateText(fileFullPath))
					{
						sw.Write(content);
					}
				}
				else
				{
					if (append)
					{
						using (StreamWriter sw = File.AppendText(fileFullPath))
						{
							sw.WriteLine(content);
						}
					}
					else
					{
						fileFullPath = fileFullPath.Replace(fi.Extension, DateTime.Now.ToShortDateString().Replace("//", "_") + fi.Extension);
						using (StreamWriter sw = File.CreateText(fileFullPath))
						{
							sw.Write(content);
						}

					}
				}
			}
			catch
			{
				return "";
			}
			return fileFullPath;
		}
	}
}
