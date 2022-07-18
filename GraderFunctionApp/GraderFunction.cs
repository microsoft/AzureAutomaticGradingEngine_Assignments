using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraderFunctionApp
{
    public static class GraderFunction
    {
        [FunctionName(nameof(AzureGraderFunction))]
        public static async Task<IActionResult> AzureGraderFunction(
             [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
             ILogger log, ExecutionContext context)
        {
            log.LogInformation("Start AzureGraderFunction");


            if (req.Method == "GET")
            {
                if (!req.Query.ContainsKey("credentials"))
                {
                    const string html = @"
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
        NUnit Test filter<br/>
        <input type='text' id='filter' name='filter' /><br/>
        <button type='submit'>Run Test</button>
    </form>
    <footer>
        <p>Developed by <a href='https://www.vtc.edu.hk/admission/en/programme/it114115-higher-diploma-in-cloud-and-data-centre-administration/'> Higher Diploma in Cloud and Data Centre Administration Team.</a></p>
    </footer>
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
                    string filter = req.Query["filter"];

                    string xml;
                    if (req.Query.ContainsKey("trace"))
                    {
                        string trace = req.Query["trace"];
                        var email = ExtractEmail(trace);
                        log.LogInformation("start:" + trace);
                        xml = await RunUnitTestProcess(context, log, credentials, email, filter);
                        log.LogInformation("end:" + trace);
                    }
                    else
                    {
                        xml = await RunUnitTestProcess(context, log, credentials, "Anonymous", filter);
                    }
                    return new ContentResult { Content = xml, ContentType = "application/xml", StatusCode = 200 };
                }

            }
            else if (req.Method == "POST")
            {
                log.LogInformation("POST Request");
                string needXml = req.Query["xml"];
                string credentials = req.Form["credentials"];
                string filter = req.Form["filter"];
                if (credentials == null)
                {
                    return new ContentResult
                    {
                        Content = $"<result><value>No credentials</value></result>",
                        ContentType = "application/xml",
                        StatusCode = 422
                    };
                }
                var xml = await RunUnitTestProcess(context, log, credentials, "Anonymous", filter);
                if (string.IsNullOrEmpty(needXml))
                {                    
                    var result = ParseNUnitTestResult(xml);
                    return new JsonResult(result);
                }
                return new ContentResult { Content = xml, ContentType = "application/xml", StatusCode = 200 };
            }

            return new OkObjectResult("ok");
        }

        private static async Task<string> RunUnitTestProcess(ExecutionContext context, ILogger log, string credentials, string trace, string filter)
        {
            var tempDir = GetTemporaryDirectory(trace);
            var tempCredentialsFilePath = Path.Combine(tempDir, "azureauth.json");

            await File.WriteAllLinesAsync(tempCredentialsFilePath, new string[] { credentials });

            string workingDirectoryInfo = Environment.ExpandEnvironmentVariables(@"%HOME%\data\Functions\Tests");
            string exeLocation = Path.Combine(workingDirectoryInfo, "AzureProjectTest.exe");
            log.LogInformation("Unit Test Exe Location: " + exeLocation);


            if (string.IsNullOrEmpty(filter))
                filter = "test==AzureProjectTest";
            else
            {
                var serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                using StreamReader r = new StreamReader(Path.Combine(workingDirectoryInfo, "tasks.json"));
                var jsonText = await r.ReadToEndAsync();
                var json = JsonConvert.DeserializeObject<List<GameTaskData>>(jsonText, serializerSettings);
                filter = json.First(c => c.Name == filter).Filter;
            }

            log.LogInformation($@"{tempCredentialsFilePath} {tempDir} {trace} {filter}");
            try
            {
                using var process = new Process();
                var info = new ProcessStartInfo
                {
                    WorkingDirectory = workingDirectoryInfo,
                    FileName = exeLocation,
                    Arguments = $@"{tempCredentialsFilePath} {tempDir} {trace} {filter}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                process.StartInfo = info;

                log.LogInformation("Refresh start.");
                process.Refresh();
                log.LogInformation("Process start.");
                var output = new StringBuilder();
                var error = new StringBuilder();
                using AutoResetEvent outputWaitHandle = new(false);
                using AutoResetEvent errorWaitHandle = new(false);
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                const int timeout = 5 * 60 * 1000;
                if (process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    // Process completed. Check process.ExitCode here.
                    log.LogInformation("Process Ended.");
                    //log.LogInformation(output.ToString());

                    var errorLog = error.ToString();
                    log.LogError(errorLog);
                    if (!string.IsNullOrEmpty(errorLog)) return null;

                    var xml = await File.ReadAllTextAsync(Path.Combine(tempDir, "TestResult.xml"));
                    Directory.Delete(tempDir, true);                   

                    return xml;
                }
                else
                {
                    // Timed out.
                    log.LogInformation("Process Timed out.");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return null;
        }

        private static string GetTemporaryDirectory(string trace)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), Math.Abs(trace.GetHashCode()).ToString());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
        public static string ExtractEmail(string content)
        {
            const string matchEmailPattern =
                @"(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
                + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
                + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
                + @"([a-zA-Z]+[\w-]+\.)+[a-zA-Z]{2,4})";

            var rx = new Regex(
                matchEmailPattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Find matches.
            var matches = rx.Matches(content);

            return matches[0].Value;

        }

        public static Dictionary<string, int> ParseNUnitTestResult(string rawXml)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(rawXml);
            return ParseNUnitTestResult(xmlDoc);
        }

        private static Dictionary<string, int> ParseNUnitTestResult(XmlDocument xmlDoc)
        {
            var testCases = xmlDoc.SelectNodes("/test-run/test-suite/test-suite/test-suite/test-case");
            var result = new Dictionary<string, int>();
            foreach (XmlNode node in testCases)
            {
                result.Add(node.Attributes?["fullname"].Value, node.Attributes?["result"].Value == "Passed" ? 1 : 0);
            }

            return result;
        }       
    }
}
