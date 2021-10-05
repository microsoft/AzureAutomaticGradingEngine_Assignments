using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using NUnit.Common;
using NUnitLite;

// This project specifies the serializer used to convert Lambda event into .NET classes in the project's main 
// main function. This assembly register a serializer for use when the project is being debugged using the
// AWS .NET Mock Lambda Test Tool.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        ///
        /// To use this handler to respond to an AWS event, reference the appropriate package from 
        /// https://github.com/aws/aws-lambda-dotnet#events
        /// and change the string input parameter to the desired event type.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandlerAsync(string credentials, ILambdaContext context)
        {
            LambdaLogger.Log("Start");
            var xml = await RunUnitTest(credentials);
            return xml;
        }


        private static async Task<string> RunUnitTest(string credentials, string folderSuffix = "")
        {
            var tempCredentialsFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

            await File.WriteAllLinesAsync(tempCredentialsFilePath, new string[] { credentials });

            var tempDir = GetTemporaryDirectory(folderSuffix);

            StringWriter strWriter = new StringWriter();
            Environment.SetEnvironmentVariable("AzureAuthFilePath", tempCredentialsFilePath);
            var autoRun = new AutoRun();
            var returnCode = autoRun.Execute(new string[]{
                "/test:AzureProjectGrader",
                "--work=" + tempDir,
                "--output=" + tempDir,
                "--err=" + tempDir,

            }, new ExtendedTextWrapper(strWriter), Console.In);
            LambdaLogger.Log(folderSuffix + " AutoRun return code:" + returnCode + " , " + tempDir);
            var xml = await File.ReadAllTextAsync(Path.Combine(tempDir, "TestResult.xml"));
            return xml;
        }
        private static string GetTemporaryDirectory(string folderSuffix)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), folderSuffix.GetHashCode().ToString());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
