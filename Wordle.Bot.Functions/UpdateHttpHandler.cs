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
using Newtonsoft.Json;
using WordleGame = Wordle.Engine.Game;
using Wordle.Engine;
using System.Text;
using BehaviourTree;
using Microsoft.Extensions.Localization;
using Wordle.Bot.Functions.Resources;
using System.Globalization;

namespace Wordle.Bot.Functions
{
    public class UpdateHttpHandler
    {
        private readonly WordleGame _game;
        private readonly IBehaviour<GameContext> _behaviour;
        private readonly IStringLocalizer<Messages> _localizer;
        private readonly TelegramBotClient _botClient;

        public UpdateHttpHandler(WordleGame game, TelegramBotClient botClient, IBehaviour<GameContext> chatBehaviour,
                                 IStringLocalizer<Messages> localizer)
        {
            _game = game;
            _behaviour = chatBehaviour;
            _localizer = localizer;
            _botClient = botClient;
        }

        [FunctionName("Update")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Update update =  JsonConvert.DeserializeObject<Update>(requestBody);
            if (update != null && update.Message != null)
            {
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(update.Message.From?.LanguageCode ?? "en-US");
                string test = _localizer["Welcome"].Value;
                var gameContext = new GameContext()
                {
                    ChatId = update.Message.Chat.Id,
                    MessageId = update.Message.MessageId,
                    Message = SanitizeInput(update.Message.Text), 
                    PlayerFirstName = update.Message.From?.FirstName ?? update.Message.From?.Username ?? "Player",
                    PlayerLastName = update.Message.From?.LastName ?? string.Empty,
                    PlayerUserName = update.Message.From?.Username ?? string.Empty,
                    Game = _game,
                    Services = new ContextServices()
                    {
                        BotClient = _botClient,
                        Localizer = _localizer
                    }
                };
                
                log.LogInformation("Received message from chat {ChatId} ", gameContext.ChatId);

                try
                {
                    var behaviourStatus = _behaviour.Tick(gameContext);
                    while (behaviourStatus == BehaviourStatus.Running)
                    {
                        behaviourStatus = _behaviour.Tick(gameContext);
                    }
                }
                catch(Exception ex)
                {
                    log.LogError(ex, "Error while processing message {Message} from chat {ChatId}", gameContext.Message, gameContext.ChatId);
                    await _botClient.SendTextMessageAsync(gameContext.ChatId, _localizer["UnexpectedError"]);
                    throw;
                }
            }
            else
            {
                if(update == null)
                {
                    log.LogWarning("Update is null. {Payload}", requestBody);
                }
                else
                {
                    log.LogWarning("Message is null. {Payload}", requestBody);
                }
            }

            return new OkResult();
        }
        
        private static string SanitizeInput(string messageText)
        {
            //TODO: evaluate if we can be target of injection or any kind of attack from user input
            messageText = messageText?.Trim() ?? string.Empty;
            int length = messageText.Length;
            return messageText.Substring(0, Math.Min(length, 100));
        }
    }
}
