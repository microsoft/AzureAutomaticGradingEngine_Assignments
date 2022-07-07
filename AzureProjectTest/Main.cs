using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Common;
using NUnitLite;

namespace AzureProjectTest
{
    class Run
    {
        static int Main(string[] args)
        {
            //var trace = "trace";
            //var tempDir = @"C:\Users\developer\Documents\test";
            //var tempCredentialsFilePath = @"C:\Users\developer\Documents\CloudLabTest.json";
            //var where = "test==AzureProjectTest.AppServiceTest.Test01_AppServicePlanWithTag";

            var tempCredentialsFilePath = args[0];
            var tempDir = args[1];
            var trace = args[2];
            var where = args[3];
            Console.WriteLine("tempCredentialsFilePath:" + tempCredentialsFilePath);
            Console.WriteLine("trace:" + trace);
            Console.WriteLine("where:" + where);

            StringWriter strWriter = new StringWriter();
            var autoRun = new AutoRun();

            var runTestParameters = new List<string>() { 
                "/test:AzureProjectTest",
                "--work=" + tempDir,
                "--output=" + tempDir,
                "--err=" + tempDir,
                "--params:AzureCredentialsPath=" + tempCredentialsFilePath + ";trace=" + trace
            };
            if (!string.IsNullOrEmpty(where))
            {
                runTestParameters.Insert(1, "--where=" + where);
            }
            var returnCode = autoRun.Execute(runTestParameters.ToArray(), new ExtendedTextWrapper(strWriter), Console.In);

            var xml = File.ReadAllText(Path.Combine(tempDir, "TestResult.xml"));
            Console.WriteLine(returnCode);
            Console.WriteLine(xml);
            return 0;
        }
        private static string GetTemporaryDirectory(string trace)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), Math.Abs(trace.GetHashCode()).ToString());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
