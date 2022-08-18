// See https://aka.ms/new-console-template for more information
using AzureProjectTest.Helper;
using System.Reflection;
using GraderFunctionApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

static IEnumerable<Type> GetTypesWithHelpAttribute(Assembly? assembly)
{
    return from Type type in assembly!.GetTypes()
           where type.GetCustomAttributes(typeof(GameClassAttribute), true).Length > 0
           select type;
}
var assembly = Assembly.GetAssembly(type: typeof(GameClassAttribute));
var allTasks = new List<GameTaskData>();
foreach (var testClass in GetTypesWithHelpAttribute(assembly))
{
    var gameClass = testClass.GetCustomAttribute<GameClassAttribute>();
    var tasks = testClass.GetMethods().Where(m => m.GetCustomAttribute<GameTaskAttribute>() != null)
        .Select(c => new { c.Name, GameTask = c.GetCustomAttribute<GameTaskAttribute>()! });

    var independentTests = tasks.Where(c => c.GameTask.GroupNumber == -1)
        .Select(c => new GameTaskData()
        {
            Name = testClass.FullName + "." + c.Name,
            Tests= new [] { testClass.FullName + "." + c.Name},
            GameClassOrder = gameClass!.Order,
            Instruction = c.GameTask.Instruction,
            Filter = "test=" + testClass.FullName + "." + c.Name,
            Reward = c.GameTask.Reward,
            TimeLimit = c.GameTask.TimeLimit
        });


    var groupedTasks = tasks.Where(c => c.GameTask.GroupNumber != -1)
        .GroupBy(c => c.GameTask.GroupNumber)
        .Select(c =>
            new GameTaskData()
            {
                Name = string.Join(" ", c.Select(a => testClass.FullName + "." + a.Name)),
                Tests = c.Select(a => testClass.FullName + "." + a.Name).ToArray(),
                GameClassOrder = gameClass!.Order,
                Instruction = string.Join("", c.Select(a => a.GameTask.Instruction)),
                Filter = string.Join("||", c.Select(a => "test==\"" + testClass.FullName + "." + a.Name+"\"")),
                Reward = c.Sum(a => a.GameTask.Reward),
                TimeLimit = c.Sum(a => a.GameTask.TimeLimit),
            }
        );


    allTasks.AddRange(independentTests);
    allTasks.AddRange(groupedTasks);
}

var serializerSettings = new JsonSerializerSettings
{
    ContractResolver = new CamelCasePropertyNamesContractResolver()
};
var json = JsonConvert.SerializeObject(allTasks.ToArray(), serializerSettings);
Console.WriteLine(json);
File.WriteAllText(@"tasks.json", json);
File.WriteAllText(@"..\..\..\..\AzureProjectTest\tasks.json", json);