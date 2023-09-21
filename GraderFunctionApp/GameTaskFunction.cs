using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azure;
using Azure.AI.OpenAI;
using AzureProjectTestLib.Helper;

using System.Runtime.Caching;

namespace GraderFunctionApp
{
    public static class GameTaskFunction
    {
        private static readonly ObjectCache TokenCache = MemoryCache.Default;
        private static async Task<string> Rephrases(string sentence)
        {
            var rnd = new Random();
            var version = rnd.Next(1, 3);
            var cacheKey = sentence + version;

            var tokenContents = TokenCache.GetCacheItem(cacheKey);
            if (tokenContents != null)
            {
                return tokenContents.Value.ToString();
            }

            var azureOpenAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var azureOpenAiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var deploymentOrModelName = Environment.GetEnvironmentVariable("DEPLOYMENT_OR_MODEL_NAME");

            if (azureOpenAiEndpoint == null || azureOpenAiApiKey == null || deploymentOrModelName == null)
                return sentence;

            var openAiClient = new OpenAIClient(
                new Uri(azureOpenAiEndpoint),
                new AzureKeyCredential(azureOpenAiApiKey)
            );
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are a Microsoft Azure game dialogue designer,Good at designing lively and interesting dialogue." +
                                                     "You only reply to instruction to ask the player setup something in Microsoft Azure."),
                    new ChatMessage(ChatRole.User,
                        $"You need to help me rewrite a sentence with the following rule:" +
                        $"1. Keep all technical teams and Noun. " +
                        $"2. It is instructions to ask player to complete tasks." +
                        $"3. In a funny style to the brave (勇者) with some emojis" +
                        $"4. In both English and Traditional Chinese." +
                        $"5. English goes first, and Chinese goes next." +
                        $"6. Only reply to the rewritten sentence, and don't answer anything else." +
                        $"Rewrite the following sentence:\n\n\n{sentence}\n"
                        ),
                },
                Temperature = (float)0.9,
                MaxTokens = 800,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            };
            var chatCompletionsResponse = await openAiClient.GetChatCompletionsAsync(
                deploymentOrModelName,
                chatCompletionsOptions
            );

            var chatMessage = chatCompletionsResponse.Value.Choices[0].Message;
            var policy = new CacheItemPolicy
            {
                Priority = CacheItemPriority.Default,
                // Setting expiration timing for the cache
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(15)
            };
            tokenContents = new CacheItem(cacheKey, chatMessage.Content);
            TokenCache.Set(tokenContents, policy);
            return chatMessage.Content;
        }

        [FunctionName("GameTaskFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var json = GetTasksJson(true);
            return new ContentResult { Content = json, ContentType = "application/json", StatusCode = 200 };
        }

        public static string GetTasksJson(bool rephrases)
        {
            {
                var assembly = Assembly.GetAssembly(type: typeof(GameClassAttribute));
                var allTasks = new List<Task<GameTaskData>>();
                foreach (var testClass in GetTypesWithHelpAttribute(assembly))
                {
                    var gameClass = testClass.GetCustomAttribute<GameClassAttribute>();
                    var tasks = testClass.GetMethods().Where(m => m.GetCustomAttribute<GameTaskAttribute>() != null)
                        .Select(c => new { c.Name, GameTask = c.GetCustomAttribute<GameTaskAttribute>()! });

                    var independentTests = tasks.Where(c => c.GameTask.GroupNumber == -1)
                        .Select(async c => new GameTaskData()
                        {
                            Name = testClass.FullName + "." + c.Name,
                            Tests = new[] { testClass.FullName + "." + c.Name },
                            GameClassOrder = gameClass!.Order,
                            Instruction = rephrases ? await Rephrases(c.GameTask.Instruction) : c.GameTask.Instruction,
                            Filter = "test=" + testClass.FullName + "." + c.Name,
                            Reward = c.GameTask.Reward,
                            TimeLimit = c.GameTask.TimeLimit
                        }).ToList();


                    var groupedTasks = tasks.Where(c => c.GameTask.GroupNumber != -1)
                        .GroupBy(c => c.GameTask.GroupNumber)
                        .Select(async c =>
                            new GameTaskData()
                            {
                                Name = string.Join(" ", c.Select(a => testClass.FullName + "." + a.Name)),
                                Tests = c.Select(a => testClass.FullName + "." + a.Name).ToArray(),
                                GameClassOrder = gameClass!.Order,
                                Instruction = rephrases ? await Rephrases(string.Join("", c.Select(a => a.GameTask.Instruction))) : string.Join("", c.Select(a => a.GameTask.Instruction)),
                                Filter =
                                    string.Join("||", c.Select(a => "test==\"" + testClass.FullName + "." + a.Name + "\"")),
                                Reward = c.Sum(a => a.GameTask.Reward),
                                TimeLimit = c.Sum(a => a.GameTask.TimeLimit),
                            }
                        ).ToList();

                    allTasks.AddRange(independentTests);
                    allTasks.AddRange(groupedTasks);
                }

                var allCompletedTask = allTasks.Select(t => t.Result).ToList();
                var serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                allCompletedTask = allCompletedTask.OrderBy(c => c.GameClassOrder).ThenBy(c => c.Tests.First()).ToList();
                var json = JsonConvert.SerializeObject(allCompletedTask.ToArray(), serializerSettings);
                return json;
            }

            static IEnumerable<Type> GetTypesWithHelpAttribute(Assembly assembly)
            {
                return from Type type in assembly!.GetTypes()
                       where type.GetCustomAttributes(typeof(GameClassAttribute), true).Length > 0
                       select type;
            }
        }
    }
}
