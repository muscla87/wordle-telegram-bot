using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BehaviourTree;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Wordle.Bot.Functions;
using Wordle.Engine;
using Wordle.Engine.Dictionaries;

namespace Wordle.Bot;

internal static class ChatBotBehaviourFunctions
{
    public static bool IsCommand(this GameContext context, string command)
    {
        return context.Message.StartsWith($"/{command}", StringComparison.OrdinalIgnoreCase) ||
               context.Message.StartsWith($"/{command} ", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsGameInProgress(this GameContext context)
    {
        return context.Game.GameEngine.GetGameEngineState().CurrentPhase != Engine.GamePhase.End;
    }

    public static BehaviourStatus LoadGame(this GameContext context)
    {
        context.Game.LoadGameStateAsync(context.ChatId).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SaveGame(this GameContext context)
    {
        context.Game.SaveGameStateAsync(context.ChatId).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SetDictionary(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var botClient = context.Services.BotClient;
        var messageParts = context.Message.Split(' ');
        if (messageParts.Length == 2)
        {
            string dictionaryName = messageParts[1];
            context.Game.UpdateDictionaryNameAsync(context.ChatId, dictionaryName).Wait();
            botClient.SendTextMessageAsync(chatId: context.ChatId,
                                            parseMode: ParseMode.MarkdownV2,
                                            text: localizer["DictionaryUpdateConfirmation"],
                                            replyMarkup: new ReplyKeyboardRemove()).Wait();
            return BehaviourStatus.Succeeded;
        }
        else
        {
            return BehaviourStatus.Failed;
        }
    }

    public static BehaviourStatus SendChangeDictionaryButtons(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var botClient = context.Services.BotClient;
         var buttons = WordsDictionaries.All.Select(x => new KeyboardButton($"/changedictionary {x.Name}")).ToArray();
        var rkm = new ReplyKeyboardMarkup(buttons);
        botClient.SendTextMessageAsync(context.ChatId, localizer["DictionaryUpdatePrompt"], replyMarkup: rkm).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendWelcomeMessage(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var botClient = context.Services.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text: string.Format(localizer["Welcome"], context.PlayerFirstName)).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SaveInitialPlayerInformation(this GameContext context)
    {
        context.Game.SaveInitialPlayerInformationAsync(context.ChatId, context.PlayerFirstName, 
                                                  context.PlayerLastName, context.PlayerUserName).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendGameAlreadyStartedMessage(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var botClient = context.Services.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: localizer["GameInProgressError"]).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus StartNewGame(this GameContext context)
    {
        context.Game.StartNewGameAsync(context.ChatId).Wait();
        return BehaviourStatus.Succeeded;
    }

public static BehaviourStatus SendInstructions(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        byte[] bytes = Convert.FromBase64String(Constants.Base64ExampleScreenshot);
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            var botClient = context.Services.BotClient;
            botClient.SendPhotoAsync(chatId: context.ChatId,
                                        photo: new InputOnlineFile(ms),
                                        parseMode: ParseMode.MarkdownV2,
                                        caption: localizer["InstructionsCaption"]).Wait();
        }
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendStatistics(this GameContext context)
    {
        var botClient = context.Services.BotClient;
        var localizer = context.Services.Localizer;
        var statisticsMessage = new StringBuilder();
        var statistics = context.Game.GetPlayerStatisticsAsync(context.ChatId).Result;
        if(statistics != null)
        {
            float winRate = statistics.PlayedGamesCount > 0 ? statistics.WonGamesCount / (float)statistics.PlayedGamesCount : 0;
            statisticsMessage.AppendLine(localizer["StatisticsDictionaryLine"].Value
                                                .Replace("{DictionaryName}", localizer[statistics.DictionaryName+"DictionaryName"]));
            
            statisticsMessage.AppendLine(localizer["StatisticsWinRateLine"].Value
                                                .Replace("{WinRatePosition}", statistics.WinRatePosition.ToString())
                                                .Replace("{GamesPlayedCount}", statistics.PlayedGamesCount.ToString())
                                                .Replace("{WinRate}", (winRate*100).ToString("N0")));

            statisticsMessage.AppendLine(localizer["StatisticsStreakLine"].Value
                                                .Replace("{BestStreakPosition}", statistics.BestStreakPosition.ToString())
                                                .Replace("{CurrentStreak}", statistics.CurrentStreak.ToString())
                                                .Replace("{BestStreak}", statistics.BestStreak.ToString()));

            statisticsMessage.AppendLine(localizer["StatisticsPointsLine"].Value
                                                .Replace("{PointsPosition}", statistics.PointsPosition.ToString())
                                                .Replace("{TotalPoints}", statistics.TotalPoints.ToString())
                                                .Replace("{AveragePoints}", statistics.AveragePoints.ToString("N2")
                                                .Replace(".", "\\.")));
            statisticsMessage.AppendLine();
            statisticsMessage.AppendLine(RenderAttemptsStatistics(statistics));

            statisticsMessage.AppendLine(localizer["StatisticsEndingLine"]);
        }
        else
        {
            statisticsMessage.AppendLine(localizer["StatisticsNoDataAvailable"]);
        }
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text: statisticsMessage.ToString()).Wait();
        return BehaviourStatus.Succeeded;
    }


    private static string RenderAttemptsStatistics(PlayerStatisticsWithPositions statistics)
    {
        StringBuilder statisticsSb = new StringBuilder();
        const int numberOfQuants = 6;
        int maxValue = statistics.GamesWonPerAttempt.Max(x => x);
        for (int i = 0; i < statistics.GamesWonPerAttempt.Length; i++)
        {
            var wonCount = statistics.GamesWonPerAttempt[i];
            var rate = wonCount / (float)maxValue;
            var filledQuantsCount = (int)Math.Floor(rate * numberOfQuants);
            var filledSquares = string.Join("", Enumerable.Range(0, filledQuantsCount).Select(x => "🟩"));
            var emptySquares = string.Join("", Enumerable.Range(0, numberOfQuants-filledQuantsCount).Select(x => "⬜️"));
            statisticsSb.AppendLine($"*{i + 1}* \\- {filledSquares}{emptySquares} {wonCount}");
        }
        return statisticsSb.ToString();
    }

    public static BehaviourStatus EvaluateWordInMessage(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var word = context.Message;
        (var result, var newState) = context.Game.GameEngine.SubmitWord(word).Result;
        if (result != Engine.WordValidationResult.Accepted)
        {   
            var errorMessage = $"🙇 Sorry, I can't accept this word";
            switch(result) {
                 case Engine.WordValidationResult.TooLong:
                    errorMessage = localizer["WordTooLongError"];
                    break;
                case Engine.WordValidationResult.TooShort:
                    errorMessage = localizer["WordTooShortError"];
                    break;
                case Engine.WordValidationResult.NotInDictionary:
                    errorMessage = string.Format(localizer["WordNotInDictionaryError"], localizer[newState.DictionaryName+"DictionaryName"]);
                    break;
            }

            using (var gameOutputImageStream = new MemoryStream())
            {
                GameBoardRenderingService.RenderGameStateAsImage(newState, gameOutputImageStream);
                gameOutputImageStream.Position = 0;
                context.Services.BotClient.SendPhotoAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        caption: errorMessage,
                                        photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(gameOutputImageStream)
                                        ).Wait();
            }
        }

        return result != Engine.WordValidationResult.Accepted ? BehaviourStatus.Failed : BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendGameSummary(this GameContext context)
    {
        var botClient = context.Services.BotClient;
        var state = context.Game.GameEngine.GetGameEngineState();
        using (var gameOutputImageStream = new MemoryStream())
        {
            GameBoardRenderingService.RenderGameStateAsImage(state, gameOutputImageStream);
            gameOutputImageStream.Position = 0;
            botClient.SendPhotoAsync(chatId: context.ChatId,
                                    photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(gameOutputImageStream)
                                    ).Wait();
        }
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendEndOfGameMessage(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var botClient = context.Services.BotClient;
        var gameState = context.Game.GameEngine.GetGameEngineState();
        var dictionaryUrlTemplate = WordsDictionaries.All.FirstOrDefault(x => x.Name == gameState.DictionaryName)?.DefinitionWebSiteUrlFormat;
        var wordToGuess = $"*[{gameState.WordToGuess}]({string.Format(dictionaryUrlTemplate, gameState.WordToGuess.ToLowerInvariant())})*";
        
        var resultMessage = gameState.IsWordGuessed ? string.Format( localizer["EndOfGameGuessed"] , wordToGuess, gameState.Attempts.Count):
                                                      string.Format( localizer["EndOfGameNotGuessed"] , wordToGuess);  
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text: resultMessage).Wait();

        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text:localizer["SendNewGameToPlayAgain"]).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendCommandsList(this GameContext context)
    {
        var localizer = context.Services.Localizer;
        var botClient = context.Services.BotClient;

        var commands = new List<string>()
        {
            "newgame", "howtoplay", "stats", "showboard", "changedictionary", "help"
        };

        StringBuilder messageText = new StringBuilder();
        foreach (var command in commands)
        {
            var commandDescriptionKey = command+"CommandDescription";
            messageText.AppendLine($"/{command} - {localizer[commandDescriptionKey]}");
        }

        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: messageText.ToString()).Wait();

        return BehaviourStatus.Succeeded;
    }
}