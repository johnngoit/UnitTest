using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTestStub
{
	public class ProjectInfo
	{
		public ProjectInfo(Dictionary<string, object> infoAll, List<ClassInfo> classLst)
		{
			this.classList = classLst;
			this.InfoAll = infoAll;
		}

		public FileInfo projectFile;
		public List<ClassInfo> classList = new List<ClassInfo>();
		Dictionary<string, object> InfoAll = new Dictionary<string, object>();
		public void parseInfo(Dictionary<string, object> infoAll)
		{
			string fileFullPath = (string)infoAll["fileFullPath"];
			string projPath = (string)infoAll["projPath"];
			FileInfo f = new FileInfo(fileFullPath);
			Dictionary<string, object> InfoAll = new Dictionary<string, object>();
			infoAll["@ProjectPath"] = projPath;
			infoAll["@ProjectName"] = projPath.Split('\\').Where(o => !string.IsNullOrWhiteSpace(o)).ToArray().Last();
			string testName = (string)infoAll["@ProjectName"] + ".Test";
			infoAll["@TestProjName"] = testName;
			infoAll["@TestPath"] = projPath + testName + "\\";
			infoAll["@TemplatePath"] = ParseInfo.getTemplatePath();
			projectFile = new FileInfo(projPath + testName + "\\" + testName + ".csproj");
		}

		public Dictionary<string, object> GenerateTestFiles(Dictionary<string, object> infoAll)
		{
			parseInfo(infoAll);
			Dictionary<string, object> projRet = new Dictionary<string, object>();
			StringBuilder sbMethodFile = new StringBuilder();
			StringBuilder sbTestClassFile = new StringBuilder();
			StringBuilder sbTestProjectFile = new StringBuilder();
			sbTestProjectFile = new StringBuilder();
			foreach (ClassInfo c in classList)
			{
				if (c.publicMethods.Count > 0)
				{
					var ret = c.GenerateTestFile(infoAll);
					foreach (var p in ret)
					{
						switch (p.Key.ToLower())
						{
							case "methodfile":
								sbMethodFile.AppendLine(p.Value.ToString());
								break;
							case "classfile":
								sbTestClassFile.AppendLine(p.Value.ToString());
								break;
							case "projectfile":
								sbTestProjectFile.AppendLine(p.Value.ToString());
								break;
							default:
								if (projRet.ContainsKey(p.Key))
								{
									var current = projRet[p.Key];
									if (current is string)
										projRet[p.Key] = current + p.Value.ToString();
									else if (current is List<FileInfo>)
									{
										foreach (var v in (List<FileInfo>)p.Value)
										{
											((List<FileInfo>)current).Add(v);
										}
										projRet[p.Key] = current;
									}
								}
								else
									projRet.Add(p.Key, p.Value);
								break;
						}
					};
				};
			}
			projRet["methodfile"]= sbMethodFile.ToString();
			projRet["classfile"]= sbTestClassFile.ToString();
			projRet["projectfile"]= sbTestProjectFile.ToString();
			List<FileInfo> l = (List<FileInfo>)projRet["generatedfiles"];
			l.Add(this.projectFile);
			projRet["generatedfiles"]= l;
			return projRet;
		}
	}
}
