using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTestStub
{
	public class MethodInfo
	{
		public MethodInfo(string v, Dictionary<string, object> infoAll)
		{
			v = v.TrimStart().Replace("{", "").TrimEnd().Replace(" partial ", " ");
			if (!v.Contains(" ( "))
				v = v.Replace("(", " (");
			if (!v.Contains(" ) "))
				v = v.Replace(")", " ) ");
			string[] methodParts = v.Replace(", ", "").Replace(" ,","").Split(' ').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
			this.methodParts = methodParts;
			string b4Name = v.Substring(0, v.IndexOf("(")).TrimEnd();
			if ((string)infoAll["@ClassName"] == methodParts[1] && b4Name.Split(' ').Count() < 3) // constructor method
			{
				this.Type = "constructor";
				this.isStatic = false;
				this.returnType = "void";
				this.Name = (string)infoAll["@ClassName"];
				this.ParamDeclare = v.Substring(v.IndexOf('(') + 1, v.IndexOf(')') - v.IndexOf('(') - 1).Replace("< ", "<").Replace(" >", ">").Replace(", ", ",");
				this.listPara = this.ParamDeclare.Split(',').ToList();
				this.ClassName = (string)infoAll["@ClassName"];
			}
			else
			{
				this.Type = "normal";
				this.isStatic = methodParts[1].Contains("static");
				int returnTypeIdx = 1;
				if (this.isStatic)
					returnTypeIdx = 2;
				this.returnType = methodParts[returnTypeIdx];
				this.Name = methodParts[returnTypeIdx + 1];
				this.ParamDeclare = v.Substring(v.IndexOf('(') + 1, v.IndexOf(')') - v.IndexOf('(') - 1).Replace("< ", "<").Replace(" >", ">").Replace(", ", ",");
				this.listPara = this.ParamDeclare.Split(',').ToList();
				this.ClassName = (string)infoAll["@ClassName"];
			}
		}

		private void parseInfo(Dictionary<string, object> infoAll)
		{
			infoAll["@MethodName"] = this.Name;
			string testMethodUnitPath = infoAll["@ClassTestFile_UnitPath"].ToString().Replace(".cs", "." + this.Name + ".g.cs");
			infoAll["@MethodTestFile_UnitPath"] = testMethodUnitPath;
			this.MethodTestFile = new FileInfo(infoAll["@TestPath"].ToString() + testMethodUnitPath);

			if (this.returnType.Contains("void"))
			{
				infoAll["@IsReturn"] = "";
				infoAll["@MethodReturnResult"] = "";
			}
			else
			{
				infoAll["@IsReturn"] = "return ";
				infoAll["@MethodReturnResult"] = "var result = ";
			}
			string[] Parames = this.ParamDeclare.Split(',').Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
			List<string> paraNames = new List<string>();
			List<string> methodParamsDeclareInitLst = new List<string>();
			foreach (string p in this.listPara)
			{
				try
				{
					paraNames.Add(p.Split(' ')[1]);
					methodParamsDeclareInitLst.Add(parseMethodParamsDeclareInit(p.TrimStart().TrimEnd()));
				}
				catch { }
			}
			//infoAll["@MethodParamsDeclareInit"] = string.Join(" = \"\";\n\t\t\t", this.listPara);
			infoAll["@MethodParamsDeclareInit"] = string.Join("\n\t\t\t", methodParamsDeclareInitLst);
			infoAll["@MethodParamsCall"] = string.Join(", ", paraNames);
			infoAll["@MethodParamDeclare"]= this.ParamDeclare;
			infoAll["@MethodReturnType"] = this.returnType;
			if (this.isStatic)
			{
				infoAll["@TargetDeclareInit"] = "";
				infoAll["@TargetDeclare"] = "";
				infoAll["@CallInstance"] = infoAll["@ClassName"];
				infoAll["@TargetParamsCall"] = "";
				
			}
			else
			{
				infoAll["@TargetDeclareInit"] = infoAll["@ClassName"] + " target = new " + infoAll["@ClassName"] + "();";
				infoAll["@TargetDeclare"] = infoAll["@ClassName"] + " target,";
				infoAll["@CallInstance"] = "target";
				infoAll["@TargetParamsCall"] = "target, ";
			}
			//@MethodParamDeclare
			//@MethodReturnType
			//@TargetDeclare
			//@CallInstance

		}

		public static string MethodTemplate_CloseTemplateStr()
		{
			return "\n\t\t}\n\t}";
		}
		private string parseMethodParamsDeclareInit(string v)
		{
			if (v.IndexOf("=") > 5)
			{
				v = v.Substring(0, v.IndexOf("="));
			}
			string ret = v + " = \"\";";
			try
			{
				v = v.Replace("  ", " ");
				var parts = v.Split(' ');
				string leftSide = "= ";
				switch (parts[0].Trim()) {
					case "string":
						leftSide += "\"" + parts[1] + "\";";
						break;
					case "int":
					case "double":
					case "float":
						leftSide += "X;";
						break;
					case "DateTime":
						leftSide += "DateTime.Now;";
						break;
					default:
						leftSide += "new " + parts[0] + "();";
						break;
				}
				ret = v + leftSide;
			}
			catch { }
			return ret;
		}

		public string Name;
		public FileInfo MethodTestFile;
		public string ClassName;

		public string Type { get; private set; }

		public bool isStatic;

		internal Dictionary<string,object> GenerateTestFile(ClassInfo classInfo,Dictionary<string, object> infoAll)
		{
			parseInfo(infoAll);
			Dictionary<string, object> ret = new Dictionary<string, object>();

			//1. generate test method files, return generated file
			string nameSpace = infoAll["@NameSpace"].ToString().Replace(infoAll["@ProjectName"].ToString(), "").Trim().Replace(".", "\\");
			FileInfo TestFile = new FileInfo(infoAll["@TestPath"].ToString() + infoAll["@MethodTestFile_UnitPath"].ToString());
			StringBuilder sbMethodFile = new StringBuilder();
			if (TestFile.Exists)
			{
				sbMethodFile = new StringBuilder(getMethodTemplate_Append(infoAll));
				Utils.WriteFile(TestFile.FullName, sbMethodFile.ToString(), true);
			}
			else
			{
				sbMethodFile = new StringBuilder(getMethodTemplateNew(infoAll));
				Utils.WriteFile(TestFile.FullName, sbMethodFile.ToString());
			}
			ret["methodfile"]= sbMethodFile.ToString();
			List<FileInfo> l = new List<FileInfo>();
			l.Add(TestFile);
			ret["generatedfiles"] = l;

			//2. generate test class file for return method declare
			StringBuilder sbClassFile = new StringBuilder(getClassTemplateAddMethod(infoAll));
			ret["classfile"]=sbClassFile.ToString();

			//3. generate test project file for return method files references
			StringBuilder sbProjectFile = new StringBuilder(getProjectAddMethodReferenceStr(infoAll));
			ret["projectfile"] = sbProjectFile.ToString();
			return ret;
		}

		private string ProjectFile_MethodFileReference = "<Compile Include=\"@MethodTestFile_UnitPath\">\n\t<DependentUpon>@ClassTestFile_UnitPath</DependentUpon>\n</Compile>";

		private string getProjectAddMethodReferenceStr(Dictionary<string, object> infoAll)
		{
			string result = "";
			try
			{
				infoAll["IsCallProjectAddMethod:" + this.MethodTestFile.Name] = true;
				var info = infoAll.Where(i => i.Key.StartsWith("@"));
				//@MethodTestFile_FullPath
				string str = ProjectFile_MethodFileReference;
				result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
			}
			catch { }
			return result;
		}

		private string getMethodTemplate_Append(Dictionary<string, object> infoAll)
		{
			string result = "";
			try
			{
				var info = infoAll.Where(i => i.Key.StartsWith("@")).ToDictionary(x => x.Key, y => y.Value);
				string[] lines = Utils.ReadFile((string)infoAll["@TemplatePath"] + "MethodTemplate_Append.txt");
				string str = string.Join(Environment.NewLine, lines);
				result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
				if (result.Contains("@MethodParamsDeclareInit"))
					result = result.Replace("@MethodParamsDeclareInit", (string)infoAll["@MethodParamsDeclareInit"]);
				if (result.Contains("@MethodReturnResult"))
					result = result.Replace("@MethodReturnResult", (string)infoAll["@MethodReturnResult"]);
			}
			catch { }
			return result;
		}

		private string getMethodTemplateNew(Dictionary<string, object> infoAll)
		{
			string result = "";
			try
			{
				var info = infoAll.Where(i => i.Key.StartsWith("@")).ToDictionary(x => x.Key, y => y.Value);
				string[] lines = Utils.ReadFile((string)infoAll["@TemplatePath"] + "MethodTemplate_New.txt");
				string str = string.Join(Environment.NewLine, lines);
				result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
				if (result.Contains("@MethodParamsDeclareInit"))
					result = result.Replace("@MethodParamsDeclareInit", (string)infoAll["@MethodParamsDeclareInit"]);
				if (result.Contains("@MethodReturnResult"))
					result = result.Replace("@MethodReturnResult", (string)infoAll["@MethodReturnResult"]);
			}
			catch { }
			return result;
		}

		private string getClassTemplateAddMethod(Dictionary<string, object> infoAll)
		{
			string result = "";
			try
			{
				var info = infoAll.Where(i => i.Key.StartsWith("@")).ToDictionary(x => x.Key, y => y.Value);
				string[] lines = Utils.ReadFile((string)infoAll["@TemplatePath"] + "ClassTemplate_AddMethod.txt");
				string str = string.Join(Environment.NewLine, lines);
				result = info.Aggregate(str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
			}
			catch { }
			return result;
		}

		public string returnType;
		public string[] methodParts;
		public string ParamDeclare;
		public List<string> listPara;
		public string methodLine;
	}
}
