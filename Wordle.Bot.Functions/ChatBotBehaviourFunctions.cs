using System;
using System.IO;
using System.Text;
using BehaviourTree;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Wordle.Bot.Functions;
using Wordle.Engine;

namespace Wordle.Bot;

internal static class ChatBotBehaviourFunctions
{
    public static bool IsCommand(this GameContext context, string command)
    {
        return context.Message.StartsWith($"/{command}", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMessage(this GameContext context, string message)
    {
        return context.Message.Equals(message, StringComparison.OrdinalIgnoreCase);
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

    public static BehaviourStatus SetCurrentCommandContext(this GameContext context, string command)
    {
        context.CurrentCommand = command;
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendWelcomeMessage(this GameContext context)
    {
        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: $"Welcome").Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus StartNewGame(this GameContext context)
    {
        context.Game.DeleteGameStateAsync(context.ChatId).Wait();
        return BehaviourStatus.Succeeded;
    }

public static BehaviourStatus SendInstructions(this GameContext context)
    {
        var instructions = @"Guess the WORD in six tries\.
Each guess must be a valid five\-letter word\. Send a message with your word to submit\.
After each guess, the shape of the characters will change to show how close your guess was to the word\.
Examples

*Start typing your guess and send it to me\!*\.";

        byte[] bytes = Convert.FromBase64String(Constants.Base64ExampleScreenshot);
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            var botClient = context.BotClient;
            botClient.SendPhotoAsync(chatId: context.ChatId,
                                        photo: new InputOnlineFile(ms),
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                                        caption: instructions).Wait();
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

    public static BehaviourStatus ResetGame(this GameContext context)
    {
        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: $"Reset Game").Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus EvaluateWordInMessage(this GameContext context)
    {
        (var result, var newState) = context.Game.GameEngine.SubmitWord(context.Message).Result;
        if (result != Engine.WordValidationResult.Accepted)
        {   
            var errorMessage = $"🙇 Sorry, I can't accept this word";
            switch(result) {
                 case Engine.WordValidationResult.TooLong:
                    errorMessage = $"🙇 Sorry, I can't accept this word because is too long";
                    break;
                case Engine.WordValidationResult.TooShort:
                    errorMessage = $"🙇 Sorry, I can't accept this word because is too short";
                    break;
                case Engine.WordValidationResult.NotInDictionary:
                    errorMessage = $"🙇 Sorry, this word is not in the game dictionary 📚 🤔";
                    break;
            }

            context.BotClient.SendTextMessageAsync(
            chatId: context.ChatId,
            replyToMessageId: context.MessageId,
            parseMode: ParseMode.MarkdownV2,
            text: errorMessage);
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
        var wordToGuess = context.Game.GameEngine.GetGameEngineState().WordToGuess;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: ParseMode.MarkdownV2,
                                        text: @$"The word to guess was *{wordToGuess}*

Send /start command to play again").Wait();
        return BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendResetConfirmationMessage(this GameContext context)
    {
        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: $"Do you want to reset all your statistics? [yes/no]").Wait();
        return BehaviourStatus.Succeeded;
    }

    

}