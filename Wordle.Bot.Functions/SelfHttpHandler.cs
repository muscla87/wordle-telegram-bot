using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;

namespace Wordle.Bot.Functions
{
    public static class SelfHttpHandler
    {
        [FunctionName("GetSelfInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP triggfgfdgger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var botClient = new TelegramBotClient(ConfigurationSettings.BotApiKey);
            var me = await botClient.GetMeAsync();
            string responseMessage = $"Hello, World! I am user {me.Id} and my name is {me.FirstName}.";

            return new OkObjectResult(responseMessage);
        }
    }
}
