using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.Json;
using WordleGame = Wordle.Engine.Game;
using Wordle.Engine;
using System.Text;
using BehaviourTree;

namespace Wordle.Bot.Functions
{
    public class UpdateHttpHandler
    {
        private readonly WordleGame _game;
        private readonly IBehaviour<GameContext> _behaviour;
        private readonly TelegramBotClient _botClient;

        public UpdateHttpHandler(WordleGame game, TelegramBotClient botClient, IBehaviour<GameContext> chatBehaviour)
        {
            _game = game;
            _behaviour = chatBehaviour;
            _botClient = botClient;
        }

        [FunctionName("Update")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP triggfgfdgger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Update update =  JsonSerializer.Deserialize<Update>(requestBody);

            var gameContext = new GameContext()
            {
                ChatId = update.Message.Chat.Id,
                MessageId = update.Message.MessageId,
                Message = update.Message.Text,
                BotClient = _botClient,
                Game = _game
            };

            var behaviourStatus = _behaviour.Tick(gameContext);
            while(behaviourStatus == BehaviourStatus.Running)
            {
                behaviourStatus = _behaviour.Tick(gameContext);
            }

            return new OkResult();
        }
    }
}
