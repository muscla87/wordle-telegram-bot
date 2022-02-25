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
        var botClient = context.BotClient;
        var messageParts = context.Message.Split(' ');
        if (messageParts.Length == 2)
        {
            string dictionaryName = messageParts[1];
            context.Game.SaveDictionaryName(context.ChatId, dictionaryName).Wait();
            botClient.SendTextMessageAsync(chatId: context.ChatId,
                                            parseMode: ParseMode.MarkdownV2,
                                            text: context.Localizer["DictionaryUpdateConfirmation"],
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
        var botClient = context.BotClient;
         var buttons = WordsDictionaries.All.Select(x => new KeyboardButton($"/changedictionary {x.Name}")).ToArray();
        var rkm = new ReplyKeyboardMarkup(buttons);
        botClient.SendTextMessageAsync(context.ChatId, context.Localizer["DictionaryUpdatePrompt"], replyMarkup: rkm).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendWelcomeMessage(this GameContext context)
    {
        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text: string.Format(context.Localizer["Welcome"], context.PlayerFirstName)).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SaveInitialPlayerInformation(this GameContext context)
    {
        context.Game.SaveInitialPlayerInformation(context.ChatId, context.PlayerFirstName, 
                                                  context.PlayerLastName, context.PlayerUserName).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendGameAlreadyStartedMessage(this GameContext context)
    {
        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: context.Localizer["GameInProgressError"]).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus StartNewGame(this GameContext context)
    {
        context.Game.StartNewGameAsync(context.ChatId).Wait();
        return BehaviourStatus.Succeeded;
    }

public static BehaviourStatus SendInstructions(this GameContext context)
    {
        byte[] bytes = Convert.FromBase64String(Constants.Base64ExampleScreenshot);
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            var botClient = context.BotClient;
            botClient.SendPhotoAsync(chatId: context.ChatId,
                                        photo: new InputOnlineFile(ms),
                                        parseMode: ParseMode.MarkdownV2,
                                        caption: context.Localizer["InstructionsCaption"]).Wait();
        }
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendStatistics(this GameContext context)
    {
        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: $"Statistics").Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus EvaluateWordInMessage(this GameContext context)
    {
        var word = context.Message;
        (var result, var newState) = context.Game.GameEngine.SubmitWord(word).Result;
        if (result != Engine.WordValidationResult.Accepted)
        {   
            var errorMessage = $"🙇 Sorry, I can't accept this word";
            switch(result) {
                 case Engine.WordValidationResult.TooLong:
                    errorMessage = context.Localizer["WordTooLongError"];
                    break;
                case Engine.WordValidationResult.TooShort:
                    errorMessage = context.Localizer["WordTooShortError"];
                    break;
                case Engine.WordValidationResult.NotInDictionary:
                    errorMessage = string.Format(context.Localizer["WordNotInDictionaryError"], context.Localizer[newState.DictionaryName+"DictionaryName"]);
                    break;
            }

            using (var gameOutputImageStream = new MemoryStream())
            {
                GameBoardRenderingService.RenderGameStateAsImage(newState, gameOutputImageStream);
                gameOutputImageStream.Position = 0;
                context.BotClient.SendPhotoAsync(chatId: context.ChatId,
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
        var botClient = context.BotClient;
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
        var botClient = context.BotClient;
        var gameState = context.Game.GameEngine.GetGameEngineState();
        var dictionaryUrlTemplate = WordsDictionaries.All.FirstOrDefault(x => x.Name == gameState.DictionaryName)?.DefinitionWebSiteUrlFormat;
        var wordToGuess = $"*[{gameState.WordToGuess}]({string.Format(dictionaryUrlTemplate, gameState.WordToGuess.ToLowerInvariant())})*";
        
        var resultMessage = gameState.IsWordGuessed ? string.Format( context.Localizer["EndOfGameGuessed"] , wordToGuess, gameState.Attempts.Count):
                                                      string.Format( context.Localizer["EndOfGameNotGuessed"] , wordToGuess);  
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text: resultMessage).Wait();

        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text:context.Localizer["SendNewGameToPlayAgain"]).Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendCommandsList(this GameContext context)
    {
        var botClient = context.BotClient;

        var commands = new List<string>()
        {
            "newgame", "howtoplay", "showboard", "changedictionary", "help"
        };

        StringBuilder messageText = new StringBuilder();
        foreach (var command in commands)
        {
            var commandDescriptionKey = command+"CommandDescription";
            messageText.AppendLine($"/{command} - {context.Localizer[commandDescriptionKey]}");
        }

        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: messageText.ToString()).Wait();

        return BehaviourStatus.Succeeded;
    }
}