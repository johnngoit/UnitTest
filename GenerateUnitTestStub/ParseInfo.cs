using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTestStub
{
	public class ParseInfo
	{
		public static Dictionary<string, string> newFiles = new Dictionary<string, string>();
		public Dictionary<string, object> Parse(string fileFullPath, string class_method = "")
		{
			string projPath = getProPathByFullPath(fileFullPath);
			if (string.IsNullOrWhiteSpace(projPath) && string.IsNullOrWhiteSpace(fileFullPath) && string.IsNullOrWhiteSpace(class_method))
			{
				FileInfo inputFile = new FileInfo("input.txt");
				string[] inputs = Utils.ReadFile(inputFile.DirectoryName.Replace("\\bin\\Debug", "\\templates\\") + inputFile.Name);
				fileFullPath = inputs[0];
				projPath = getProPathByFullPath(inputs[0]);
				if (inputs.Length > 1)
					class_method = inputs[1];
			}
			Dictionary<string, object> infoAll = new Dictionary<string, object>();
			infoAll["fileFullPath"] = fileFullPath;
			infoAll["projPath"] = projPath;
			//FileInfo f = new FileInfo(fileFullPath);
			//Dictionary<string, object> infoAll = new Dictionary<string, object>();
			//infoAll["@ProjectPath"] = projPath;
			//infoAll["@ProjectName"] = projPath.Split('\\').Where(o => !string.IsNullOrWhiteSpace(o)).ToArray().Last();
			//string testName = (string)infoAll["@ProjectName"] + ".Test";
			//infoAll["@TestProjName"] = testName;
			//infoAll["@TestPath"] = projPath + testName + "\\";
			//infoAll["@FileClassName"] = f.Name.Replace(f.Extension, "");
			//if (!string.IsNullOrWhiteSpace(class_method))
			//	infoAll["@ClassName"] = class_method.Split('.')[0];
			//else
			//	infoAll["@ClassName"] = infoAll["@FileClassName"];
			//if (string.IsNullOrWhiteSpace(fileFullPath))
			//	fileFullPath = projPath + infoAll["@NameSpace"].ToString().Replace(".","\\") + "\\" + infoAll["@ClassName"] + ".cs";
			//infoAll["fileFullPath"] = fileFullPath;			
			//infoAll["@NameSpace"] = Utils.ReadFile(fileFullPath).Where(j => isNameSpaceLine(j)).FirstOrDefault().Trim().Replace("namespace","");

			List<ClassInfo> classList = new List<ClassInfo>();
			var lines = Utils.ReadFile(fileFullPath);
			ClassInfo currentClass = null;
			List<MethodInfo> methods = new List<MethodInfo>();
			for (int i = 0; i < lines.Length;i++)
			{
				if (isLineAClassDeclare(lines[i])) {
					if (currentClass != null)
					{
						currentClass.publicMethods = methods;
						classList.Add(currentClass);
						methods = new List<MethodInfo>();
					}
					currentClass = new ClassInfo(lines[i], infoAll);
				}

				if (isLineAMethodDeclare(lines[i], "")){
					methods.Add(new MethodInfo(lines[i], infoAll));
				}
			}

			if (currentClass != null)
			{
				currentClass.publicMethods = methods;
				classList.Add(currentClass);
			}

			//GenerateUnitFiles1(infoAll, classMethodsDic);
			ProjectInfo p = new ProjectInfo(infoAll, classList);
			var ret = p.GenerateTestFiles(infoAll);
			writeProject(ret, infoAll);
			return infoAll;
		}

		private void writeProject(Dictionary<string, object> ret, Dictionary<string, object> infoAll)
		{
			StringBuilder output = new StringBuilder();
			foreach (var c in ret)
			{
				output.AppendLine(c.Key);
				if ( c.Value is List<FileInfo>)
					foreach(var i in (List<FileInfo>)c.Value)						
						output.AppendLine(i.FullName);
				else
					output.AppendLine(c.Value.ToString());
			}
			Utils.WriteFile((string)infoAll["@TemplatePath"] + "output.txt", output.ToString(), true);
		}

		//private void parseInfo(Dictionary<string, object> infoAll)
		//{
		//	ClassInfo cInfo = new ClassInfo();
		//	StringBuilder output = new StringBuilder();
		//	StringBuilder projOutput = new StringBuilder();
		//	FileInfo f = new FileInfo((string)infoAll["fileFullPath"]);
		//	string className = infoAll["@ClassName"].ToString();
		//	string projectName = infoAll["@ProjectName"].ToString();

		//	int classNameIndex = f.FullName.IndexOf(projectName + "\\");
		//	string ClassTestFile_UnitPath = "UnitTests\\" + f.FullName.Substring(classNameIndex);
		//	infoAll["@ClassTestFile_UnitPath"] = ClassTestFile_UnitPath.Replace(f.Extension, "Test" + f.Extension);
		//	f = new FileInfo((string)infoAll["@ClassTestFile_UnitPath"]);
		//	newFiles["Project"] = (string)infoAll["@TestPath"] + (string)infoAll["@TestProjName"] + ".csproj";

		//	string testRelPath = infoAll["@NameSpace"].ToString().Replace(infoAll["@ProjectName"].ToString() + ".", "").Replace(".", "\\").Trim();
		//	string fileMiddlePart = (string)infoAll["@FileClassName"];
		//	className = (string)infoAll["@ClassName"];
		//	if (fileMiddlePart != className)
		//	{
		//		fileMiddlePart += "." + className;
		//	}
		//	fileMiddlePart += "Test.";
		//	string testClassFileName = fileMiddlePart + "cs";
		//	string testClassUnitPath = "UnitTests\\" + testRelPath + "\\" + testClassFileName;
		//	newFiles["@Class:" + className] = (string)infoAll["@TestPath"] + testClassUnitPath;
		//	infoAll["@ClassTestFile_UnitPath"] = testClassUnitPath;
		//}

		private string getProPathByFullPath(string fileFullPath)
		{
			//C:\websites\dnndev.me\DesktopModules\SharedLibrary\Common\RSSGenerator.cs
			//C:\websites\dnndev.me\DesktopModules\SharedLibrary
			List<string> fileParts = fileFullPath.Split(Path.DirectorySeparatorChar).Select(p => p.Trim()).ToList();
			if (string.IsNullOrWhiteSpace(fileFullPath))
				return "";
			else {
				string projectName = fileParts[fileParts.IndexOf("DesktopModules") + 1];
				string proPath = fileFullPath.Substring(0, fileFullPath.IndexOf(projectName));
				return proPath + projectName + "\\";
			}
		}

		public static bool isNameSpaceLine(string line)
		{
			return line.StartsWith("namespace ");
		}

		private void GenerateUnitFiles(Dictionary<string, object> infoAll, string[] methodList)
		{
			ClassInfo cInfo = new ClassInfo();
			StringBuilder output = new StringBuilder();
			StringBuilder projOutput = new StringBuilder();
			FileInfo f = new FileInfo((string)infoAll["fileFullPath"]);
			string className = infoAll["@ClassName"].ToString();
			string projectName = infoAll["@ProjectName"].ToString();

			int classNameIndex = f.FullName.IndexOf(projectName + "\\");			
			string ClassTestFile_UnitPath = "UnitTests\\" + f.FullName.Substring(classNameIndex);
			infoAll["@ClassTestFile_UnitPath"] = ClassTestFile_UnitPath.Replace( f.Extension, "Test" + f.Extension);
			f = new FileInfo((string)infoAll["@ClassTestFile_UnitPath"]);
			newFiles["Project"] = (string)infoAll["@TestPath"] + (string)infoAll["@TestProjName"] + ".csproj";

			string testRelPath = infoAll["@NameSpace"].ToString().Replace(infoAll["@ProjectName"].ToString() + ".", "").Replace(".", "\\").Trim();
			string fileMiddlePart = (string)infoAll["@FileClassName"];
			className = (string)infoAll["@ClassName"];
			if (fileMiddlePart != className)
			{
				fileMiddlePart += "." + className;
			}
			fileMiddlePart += "Test.";
			string testClassFileName = fileMiddlePart + "cs";
			string testClassUnitPath = "UnitTests\\" + testRelPath + "\\" + testClassFileName;
			newFiles["@Class:" + className] = (string)infoAll["@TestPath"] + testClassUnitPath;
			infoAll["@ClassTestFile_UnitPath"] = testClassUnitPath;

			projOutput.AppendLine(cInfo.getProjectClassTemplateStr(infoAll));

			infoAll["@TemplatePath"] = getTemplatePath();
			StringBuilder sbProjAddMethod = new StringBuilder();
			StringBuilder sbClassAddMethod = new StringBuilder(cInfo.getClassTemplateNewStr(infoAll));
			StringBuilder sbMethod = new StringBuilder();
			//for (int i = 0; i < methodList.Length; i++){
			//	MethodInfo mi = parseMethodInfo(methodList[i]);
			//	infoAll["@MethodReturnType"] = mi.returnType;
			//	infoAll["@MethodName"] = mi.Name;
			//	if (mi.isStatic)
			//	{
			//		infoAll["@TargetDeclare"] = "";
			//		infoAll["@CallInstance"] = infoAll["@ClassName"];
			//	}
			//	else
			//	{
			//		infoAll["@TargetDeclare"] = infoAll["@ClassName"] + " target,";
			//		infoAll["@CallInstance"] = "target";
			//	}

			//	infoAll["@MethodParamDeclare"] = mi.ParamDeclare;
			//	//@MethodParamsDeclareInit
			//	//@MethodReturnResult
			//	if (mi.returnType.Contains("void"))
			//	{
			//		infoAll["@IsReturn"] = "";
			//		infoAll["@MethodReturnResult"] = "";
			//	}
			//	else
			//	{
			//		infoAll["@IsReturn"] = "return ";
			//		infoAll["@MethodReturnResult"] = "\tvar result = ";
			//	}
			//	infoAll["@MethodParamsDeclareInit"] = string.Join(" = null;\n\t", mi.listPara);
			//	List<string> paraNames = new List<string>();
			//	foreach (string p in mi.listPara)
			//	{
			//		try
			//		{
			//			paraNames.Add(p.Split(' ')[1]);
			//		}
			//		catch { }
			//	}
			//	string[] Parames = mi.ParamDeclare.Split(',').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
			//	infoAll["@MethodParamsCall"] = string.Join(", ", paraNames);
			//	string testMethodFileName = fileMiddlePart + mi.Name + ".g.cs";
			//	string testMethodUnitPath = "UnitTests\\" + testRelPath + "\\" + testMethodFileName;
			//	infoAll["@MethodTestFile_UnitPath"] = testMethodUnitPath;
			//	newFiles["Method:" + mi.Name] = (string)infoAll["@TestPath"] + testMethodUnitPath;
			//	//1. getProjectAddMethodStr
			//	if (!infoAll.ContainsKey("IsCallProjectAddMethod:" + mi.Name))
			//		sbProjAddMethod.AppendLine(getProjectAddMethodReferenceStr(infoAll, mi));
				
			//	//2. getClassAddMethodStr
			//	sbClassAddMethod.AppendLine(getClassTemplateAddMethod(infoAll, mi));

			//	//3. getMethodString
			//	sbMethod.AppendLine("//------------------------");
			//	sbMethod.AppendLine("//FileName:" + newFiles["Method:" + mi.Name]);
			//	sbMethod.AppendLine(getMethodTemplateNew(infoAll, mi));
			//}
			projOutput.AppendLine(sbProjAddMethod.ToString());
			output.AppendLine(projOutput.ToString());


			output.AppendLine(sbClassAddMethod.ToString());
			output.AppendLine(sbMethod.ToString());
			output.AppendLine(string.Join(Environment.NewLine + "\\", newFiles));
			Utils.WriteFile((string)infoAll["@TemplatePath"] + "output.txt", output.ToString());
		}

		public static string[] loadTemplate(string[] lines, string templatePath)
		{  //C:\dev\github\GenerateUnitTestStub\GenerateUnitTestStub\
		   //C:\dev\github\GenerateUnitTestStub\GenerateUnitTestStub\bin\Debug
		   //C:\dev\github\GenerateUnitTestStub\GenerateUnitTestStub\templates
			try
			{
				for (int i = 0; i < lines.Length; i++)
				{
					bool isTemplateLine = lines[i].Trim().StartsWith("@Template:");
					if (isTemplateLine)
					{
						string templateFileFullPath = templatePath + lines[i].Trim().Replace("@Template:", "");
						lines[i] = string.Join(Environment.NewLine, Utils.ReadFile(templateFileFullPath));
					}
				}
			}
			catch { }
			return lines;
		}

		public static string getTemplatePath()
		{
			FileInfo f = new FileInfo("tmp.txt");
			return f.DirectoryName.Replace("\\bin\\Debug", "\\templates") + "\\";
		}

		private bool isLineAClassDeclare(string line)
		{
			line = line.TrimStart().Replace("{", "").TrimEnd().Replace(" partial ", " ");
			if (line.IndexOf(":") > 5)
			{
				line = line.Substring(0, line.IndexOf(":")).TrimEnd();
			}
			bool isAClass = line.Contains(" class ");
			if (!isAClass)
				return false;
			var part = line.Split(' ');
			if (part.Count() < 2)
				return false;
			bool isStartwithPublic = line.TrimStart().StartsWith("public ");
			if (!isStartwithPublic)
				return false;
			bool isStaticClass = part[1].Trim().Contains("static");
			bool isPartsCount = (isStaticClass && part.Length == 4) || (part.Length == 3);
			return isStartwithPublic && isAClass && isPartsCount;
		}
		private bool isLineAMethodDeclare(string v, string methodName)
		{
			v = v.TrimStart().TrimEnd().Replace("( ", "(").Replace(" )", ")").Replace("{", "");
			bool isContainMethodName = string.IsNullOrWhiteSpace(methodName) || v.Trim().Contains(methodName + "(");
			if (!isContainMethodName)
				return false;
			bool isLineStartWithEncapsulate = v.Trim().StartsWith("public") || v.StartsWith("protected");
			if (!isLineStartWithEncapsulate)
				return false;
			bool isLineFunctionIndicate = v.Contains("(") && v.Trim().EndsWith(")");
			var nameParts = v.Split(' ');
			//if (nameParts.Count() < 3)
			//	return false;
			return isContainMethodName && isLineStartWithEncapsulate && isLineFunctionIndicate;
		}

		//private void GenerateUnitFiles1(Dictionary<string, object> infoAll, List<ClassInfo> classes)
		//{
		//	foreach (ClassInfo c in classes)
		//	{
		//		c.GenerateTestFile(infoAll);
		//	}
		//}
		//public static string ProjectTemplate_ClassTestFile = "<Compile Include=\"@ClassTestFile_UnitPath\" />";
		//public static string ProjectTemplate_MethodTestFile = "<Compile Include=\"@MethodTestFile_UnitPath\">\n\t<DependentUpon>@ClassTestFile_UnitPath</DependentUpon>\n</Compile>";

		//private string getMethodTemplateNew(Dictionary<string, object> infoAll, MethodInfo mi)
		//{
		//	string result = "";
		//	try
		//	{
		//		var info = infoAll.Where(i => i.Key.StartsWith("@")).ToDictionary(x => x.Key, y => y.Value);
		//		string[] lines = Utils.ReadFile((string)infoAll["@TemplatePath"] + "MethodTemplate_New.txt");
		//		string str = string.Join(Environment.NewLine, lines);
		//		result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
		//		if (result.Contains("@MethodParamsDeclareInit"))
		//			result = result.Replace("@MethodParamsDeclareInit", (string)infoAll["@MethodParamsDeclareInit"]);
		//		if (result.Contains("@MethodReturnResult"))
		//			result = result.Replace("@MethodReturnResult", (string)infoAll["@MethodReturnResult"]);
		//	}
		//	catch { }
		//	return result;
		//}

		//private string getClassTemplateAddMethod(Dictionary<string, object> infoAll, MethodInfo mi)
		//{ string result = "";
		//	try
		//	{
		//		var info = infoAll.Where(i => i.Key.StartsWith("@")).ToDictionary(x => x.Key, y => y.Value);
		//		string[] lines = Utils.ReadFile((string)infoAll["@TemplatePath"] + "ClassTemplate_AddMethod.txt");
		//		string str = string.Join(Environment.NewLine, lines);
		//		result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
		//	}
		//	catch { }
		//	return result;
		//}

		//private string getClassTemplateNewStr(Dictionary<string, object> infoAll)
		//{
		//	string result = "";
		//	try
		//	{
		//		var info = infoAll.Where(i => i.Key.StartsWith("@"));
		//		string templatePath = (string)infoAll["@TemplatePath"];
		//		string[] lines = loadTemplate(Utils.ReadFile(templatePath + "ClassTemplate_New.txt"), templatePath);
		//		string str = string.Join(Environment.NewLine, lines);
		//		result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
		//	}
		//	catch { }
		//	return result;
		//}

		//private string getProjectAddMethodReferenceStr(Dictionary<string, object> infoAll, MethodInfo mi)
		//{
		//	string result = "";
		//	try
		//	{
		//		infoAll["IsCallProjectAddMethod:" + mi.Name] = true;
		//		var info = infoAll.Where(i => i.Key.StartsWith("@"));
		//		//@MethodTestFile_FullPath
		//		string str = ProjectTemplate_MethodTestFile;
		//		result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
		//	}
		//	catch { }
		//	return result;
		//}

		//private string getProjectClassTemplateStr(Dictionary<string, object> infoAll) {
		//	string result = "";
		//	try
		//	{
		//		var info = infoAll.Where(i => i.Key.StartsWith("@"));
		//		string str = ProjectTemplate_ClassTestFile;
		//		result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
		//	}
		//	catch { }
		//	return result;
		//}

		//private MethodInfo parseMethodInfo(string v)
		//{
		//	if (!v.Contains(" ( "))
		//		v = v.Replace("(", " (");
		//	if (!v.Contains(" ) "))
		//		v = v.Replace(")", " ) ");
		//	string[] methodParts = v.Split(' ').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
		//	MethodInfo mi = new MethodInfo();
		//	mi.methodParts = methodParts;
		//	mi.isStatic = methodParts[1].Contains("static");
		//	int returnTypeIdx = 1;
		//	if (mi.isStatic)
		//		returnTypeIdx = 2;
		//	mi.returnType = methodParts[returnTypeIdx];
		//	mi.Name = methodParts[returnTypeIdx + 1];
		//	mi.ParamDeclare = v.Substring(v.IndexOf('(') + 1, v.IndexOf(')') - v.IndexOf('(') - 1).Replace("< ", "<").Replace(" >", ">").Replace(", ", ",");
		//	mi.listPara = mi.ParamDeclare.Split(',').ToList();
		//	return mi;
		//}
	}
}
