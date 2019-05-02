using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateUnitTestStub
{
	class Program
	{
		static void Main(string[] args)
		{
			string[] info = new string[] { @"C:\websites\dnndev.me\DesktopModules\SharedLibrary\", @"C:\websites\dnndev.me\DesktopModules\SharedLibrary\Common\RSSGenerator.cs" };
			ParseInfo p = new ParseInfo();
			Dictionary<string, object> inFo = p.Parse("","");

			// The code provided will print ‘Hello World’ to the console.
			// Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
			//Console.WriteLine("Hello World!");
			//Console.ReadKey();
			// Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 

			// 0. $projectbasePath = C:\websites\www.dnn8dev.me\DesktopModules\SharedLibrary
			//1. Test $ProjectName ProjectName = SharedLibrary.Test
			//2. Test $relativeMethodLocation NameSpace.Class = Common.RSSGeneratorChannel
			//2a. $relativeMethodPath = Common
			//2b. $class = RSSGeneratorChannel
			//2c. $MethodInfo = Name = AddItem, return type = RSSGeneratorItem, Parameters = string title, string description, string author, string link, DateTime pubDate, string guid, List<string> categories = null
			//3. Test MethodDropLocation = $projectbasePath + $ProjectName + "UnitTests" + $relativeMethodLocationWithoutClass_Method
		}
	}
}
