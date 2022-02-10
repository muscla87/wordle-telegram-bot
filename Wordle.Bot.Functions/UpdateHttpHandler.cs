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

namespace Wordle.Bot.Functions
{
    public class UpdateHttpHandler
    {
        private readonly WordleGame _game;

        public UpdateHttpHandler(WordleGame game)
        {
            _game = game;
        }

        [FunctionName("Update")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP triggfgfdgger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Update update =  JsonSerializer.Deserialize<Update>(requestBody);

            //TODO: inject as singleton
            var botClient = new TelegramBotClient(ConfigurationSettings.BotApiKey);
            if(update.Message != null) 
            {
                var chatId = update.Message.Chat.Id;
                var messageText = update.Message.Text;


                var command = ParseMessage(messageText);
                if (command == CommandType.Word)
                {
                    await _game.LoadGameStateAsync(chatId);
                    if (_game.GameEngine.GetGameEngineState().CurrentPhase == Engine.GamePhase.End)
                    {
                        Message messageSent = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Next word will be available in 01:12:23");
                        var state = _game.GameEngine.GetGameEngineState();
                        messageSent = await botClient.SendTextMessageAsync(
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                        chatId: chatId,
                        text: RenderStatus(state));
                    }
                    else
                    {
                        (var result, var newState) = await _game.GameEngine.SubmitWord(messageText);
                        if (result != Engine.WordValidationResult.Accepted)
                        {
                            Message sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            replyToMessageId: update.Message.MessageId,
                            text: $"Please provide a valid word. Reason: {result}");
                        }
                        else
                        {
                            await _game.SaveGameStateAsync(chatId);
                            Message sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                            text: RenderStatus(newState));
                        }
                    }
                }
            }

            return new OkResult();
        }

        private string RenderStatus(GameEngineState gameState) 
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("`");

            for (int i = 0; i < gameState.Attempts.Count; i++)
            {
                for (int c = 0; c < gameState.Attempts[0].Length; c++)
                {
                    strBuilder.Append(" " + gameState.Attempts[i].ToUpperInvariant()[c] + " ");
                }
                strBuilder.AppendLine();
                for (int c = 0; c < gameState.Attempts[0].Length; c++)
                {
                    strBuilder.Append("" + GetMatchSymbol(gameState.AttemptsMask[i][c]) + " ");
                }
                strBuilder.AppendLine();
                strBuilder.AppendLine();
            }
            strBuilder.Append("`");
            return strBuilder.ToString();
        }

        private string GetMatchSymbol(PositionMatchMask positionMatchMask)
        {
            switch (positionMatchMask)
            {
                case PositionMatchMask.NotMatched: return "⬜";
                case PositionMatchMask.Matched: return "🟩";
                case PositionMatchMask.MatchInOtherPosition: return "🟨";
                default: return string.Empty;
            }
        }

        private CommandType ParseMessage(string message)
        {
            return CommandType.Word;
        }

        public enum CommandType
        {
            Word,
            GameReview,
            Statistics,
            Reset
        }
    }
}
