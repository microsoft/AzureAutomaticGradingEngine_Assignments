using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NUnitLite;
using NUnit.Common;

namespace AzureProjectGraderFunctionApp
{
    public static class GraderFunction
    {
        [FunctionName("AzureGraderFunction")]
        public static async Task<IActionResult> Run(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
             ILogger log, ExecutionContext context)
        {
            log.LogInformation("Start AzureGraderFunction");


            if (req.Method == "GET")
            {
                if (!req.Query.ContainsKey("credentials"))
                {
                    string html = @"
<!DOCTYPE html>

<html lang='en' xmlns='http://www.w3.org/1999/xhtml'>
<head>
    <meta charset='utf-8' />
    <title>Azure Grader</title>
</head>
<body>
    <form id='contact-form' method='post'>
        Azure Credentials<br/>
        <textarea name='credentials' required  rows='15' cols='100'></textarea>
        <br/>
        <button type='submit'>Run Test</button>
    </form>
</body>
</html>";


                    return new ContentResult()
                    {
                        Content = html,
                        ContentType = "text/html",
                        StatusCode = 200,
                    };
                }
                else
                {
                    string credentials = req.Query["credentials"];

                    string xml = "";
                    if (req.Query.ContainsKey("trace"))
                    {
                        string trace = req.Query["trace"];
                        log.LogInformation("start:" + trace);
                        xml = await RunUnitTest(log, credentials, trace);
                        log.LogInformation("end:" + trace);
                    }
                    else
                    {
                        xml = await RunUnitTest(log, credentials);
                    }
                    return new ContentResult { Content = xml, ContentType = "application/xml", StatusCode = 200 };
                }

            }
            else if (req.Method == "POST")
            {
                log.LogInformation("POST Request");

                log.LogInformation("Form Submit");
                string credentials = req.Form["credentials"];
                if (credentials == null)
                {
                    return new ContentResult
                    {
                        Content = $"<result><value>No credentials</value></result>",
                        ContentType = "application/xml",
                        StatusCode = 422
                    };
                }
                var xml = await RunUnitTest(log, credentials);
                return new ContentResult { Content = xml, ContentType = "application/xml", StatusCode = 200 };
            }

            return new OkObjectResult("ok");
        }


        private static async Task<string> RunUnitTest(ILogger log, string credentials, string folderSuffix = "")
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
            log.LogInformation(folderSuffix + " AutoRun return code:" + returnCode + " , " + tempDir);
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
