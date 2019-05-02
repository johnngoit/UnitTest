using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTestStub
{
	public class ClassInfo
	{
		public ClassInfo() { }
		public ClassInfo(string line, Dictionary<string, object> infoAll)
		{
			line = line.TrimStart().Replace("{", "").TrimEnd().Replace(" partial ", " ");
			var parts = line.Split(' ');
			isStatic = parts[1].Trim().Contains("static");
			string className = parts[2];
			if (isStatic)
				className = parts[3];
			ClassFile = new FileInfo(className + ".cs");
			infoAll["@ClassName"] = className;
			if (infoAll != null) {
				if (infoAll.ContainsKey("@NameSpace"))
					nameSpace = (string)infoAll["@NameSpace"].ToString().Trim();
			}
		}

		private void parseInfo(Dictionary<string, object> infoAll)
		{
			//infoAll["@FileClassName"] = f.Name.Replace(f.Extension, "");
			//if (!string.IsNullOrWhiteSpace(class_method))
			//	infoAll["@ClassName"] = class_method.Split('.')[0];
			//else
			//	infoAll["@ClassName"] = infoAll["@FileClassName"];
			//if (string.IsNullOrWhiteSpace(fileFullPath))
			//fileFullPath = projPath + infoAll["@NameSpace"].ToString().Replace(".", "\\") + "\\" + infoAll["@ClassName"] + ".cs";
			
			string className = this.ClassFile.Name.Replace(".cs", "");
			infoAll["@ClassName"] = className;
			string fileFullPath= (string)infoAll["fileFullPath"];
			FileInfo f = new FileInfo(fileFullPath);
			infoAll["@NameSpace"] = Utils.ReadFile(fileFullPath).Where(j => ParseInfo.isNameSpaceLine(j)).FirstOrDefault().Trim().Replace("namespace ", "");
			string projName = (string)infoAll["@ProjectName"];
			int projNameIndex = fileFullPath.IndexOf(projName) + projName.Length + 1;
			int classNameIndex = fileFullPath.IndexOf(f.Name);
			string testRelPath = "";
			if (classNameIndex > projNameIndex) {
				testRelPath = fileFullPath.Substring(projNameIndex, classNameIndex - projNameIndex);
			}
			string testClassUnitPath = "UnitTests\\" + testRelPath + ClassFile.Name.Replace(".cs", "Test.cs");
			infoAll["@ClassTestFile_UnitPath"] = testClassUnitPath;
			ClassTestFile = new FileInfo(infoAll["@TestPath"].ToString() + testClassUnitPath);
		}

		public string nameSpace;
		public FileInfo ClassFile;
		public FileInfo ClassTestFile;
		public bool isStatic;
		public List<MethodInfo> publicMethods = new List<MethodInfo>();

		public static string ProjectFile_ClassFileReference = "<Compile Include=\"@ClassTestFile_UnitPath\" />\n";
		public Dictionary<string,object> GenerateTestFile(Dictionary<string, object> infoAll)
		{
			parseInfo(infoAll);
			Dictionary<string, object> classRet = new Dictionary<string, object>();
			if (publicMethods.Count > 0)
			{
				StringBuilder sbMethodFile = new StringBuilder();
				StringBuilder sbMethodFile_ReturnMethods = new StringBuilder();
				StringBuilder sbTestClassFile = new StringBuilder();
				StringBuilder sbTestProjectFile = new StringBuilder();
				StringBuilder sbTestClassFile_fromMethodReturn = new StringBuilder();
				sbTestClassFile = new StringBuilder(getClassTemplateNewStr(infoAll));
				sbTestProjectFile = new StringBuilder(getProjectClassTemplateStr(infoAll));
				foreach (MethodInfo m in publicMethods)
				{
					var mReturn = m.GenerateTestFile(this, infoAll);
					foreach(var p in mReturn)
					{
						switch (p.Key.ToLower())
						{
							case "methodfile":
								sbMethodFile_ReturnMethods.AppendLine(p.Value.ToString());
								break;
							case "classfile":
								sbTestClassFile_fromMethodReturn.AppendLine(p.Value.ToString());
								break;
							case "projectfile":
								sbTestProjectFile.AppendLine(p.Value.ToString());
								break;
							default:
								if (classRet.ContainsKey(p.Key))
								{
									var current = classRet[p.Key];
									if (current is string)
										classRet[p.Key] = current + p.Value.ToString();
									else if (current is List<FileInfo>)
									{
										foreach (var v in (List<FileInfo>)p.Value)
										{
											((List<FileInfo>)current).Add(v);
										}
										classRet[p.Key] = current;
									}
								}
								else
									classRet.Add(p.Key, p.Value);
								break;
						}
					};
				}
				sbTestClassFile.Replace("@sbTestClassFile_fromMethodReturn", sbTestClassFile_fromMethodReturn.ToString());
				classRet["methodfile"] = sbMethodFile_ReturnMethods.ToString();
				classRet["classfile"] = sbTestClassFile.ToString();
				Utils.WriteFile(ClassTestFile.FullName, sbTestClassFile.ToString());
				List<FileInfo> l = (List<FileInfo>)classRet["generatedfiles"];
				l.Add(this.ClassTestFile);
				classRet["generatedfiles"] = l;
				classRet["projectfile"]= sbTestProjectFile.ToString();
			}
			return classRet;			
		}
		public string getProjectClassTemplateStr(Dictionary<string, object> infoAll)
		{
			string result = "";
			try
			{
				var info = infoAll.Where(i => i.Key.StartsWith("@"));
				string str = ProjectFile_ClassFileReference;
				result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
			}
			catch { }
			return result;
		}

		public string getClassTemplateNewStr(Dictionary<string, object> infoAll)
		{
			string result = "";
			try
			{
				var info = infoAll.Where(i => i.Key.StartsWith("@"));
				string templatePath = (string)infoAll["@TemplatePath"];
				string[] lines = ParseInfo.loadTemplate(Utils.ReadFile(templatePath + "ClassTemplate_New.txt"), templatePath);
				string str = string.Join(Environment.NewLine, lines);
				result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
			}
			catch { }
			return result;
		}

	}
}
