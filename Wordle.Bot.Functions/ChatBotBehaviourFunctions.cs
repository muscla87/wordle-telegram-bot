using System;
using System.Text;
using BehaviourTree;
using Telegram.Bot;
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

🅦ＥＡＲＬＹ 
The letter *W* is in the word and in the correct spot\.

Ｐ🄸ＬＬＳ
The letter *I* is in the word but in the wrong spot\.

🅢🅚Ｉ🅛🅛  
The letter *I* is not in the word in any spot\.

*Start typing your guess and send it to me\!*\.";

        var botClient = context.BotClient;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                                        text: instructions).Wait();
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
            context.BotClient.SendTextMessageAsync(
            chatId: context.ChatId,
            replyToMessageId: context.MessageId,
            text: $"Please provide a valid word. Reason: {result}");
        }

        return result != Engine.WordValidationResult.Accepted ? BehaviourStatus.Failed : BehaviourStatus.Succeeded;
    }

    public static BehaviourStatus SendGameSummary(this GameContext context)
    {
        var botClient = context.BotClient;
        var state = context.Game.GameEngine.GetGameEngineState();
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                                        text: RenderStatus(state)).Wait();
        return BehaviourStatus.Succeeded;

        string RenderStatus(GameEngineState gameState) 
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("`");

            for (int i = 0; i < gameState.Attempts.Count; i++)
            {
                // for (int c = 0; c < gameState.Attempts[0].Length; c++)
                // {
                //     // strBuilder.Append(" ");
                //     // if(gameState.AttemptsMask[i][c] != PositionMatchMask.NotMatched)
                //     // strBuilder.Append("*");
                //     // strBuilder.Append("" + gameState.Attempts[i].ToUpperInvariant()[c] + "");
                //     // if(gameState.AttemptsMask[i][c] != PositionMatchMask.NotMatched)
                //     // strBuilder.Append("*");
                //     strBuilder.Append(GetMatchSymbol(gameState.AttemptsMask[i][c]));
                //     strBuilder.Append(" ");
                    
                // }

                // strBuilder.AppendLine();

                for (int c = 0; c < gameState.Attempts[0].Length; c++)
                {
                    //strBuilder.Append("" + GetMatchSymbol(gameState.AttemptsMask[i][c]) + "");
                    strBuilder.Append(GetLetterSymbol(gameState.Attempts[i].ToUpperInvariant()[c], gameState.AttemptsMask[i][c]));
                    strBuilder.Append(" ");
                }
                strBuilder.AppendLine();
            }
            strBuilder.Append("`");
            return strBuilder.ToString();
        }

        Rune GetLetterSymbol(char letter, PositionMatchMask positionMatchMask)
        {
            int offset = letter - 'A';
            return new Rune(0xFF21 + offset);
            switch (positionMatchMask)
            {
                case PositionMatchMask.Matched: return new Rune(0xFF21 + offset);//filled circle
                case PositionMatchMask.NotMatched: return new Rune(0x1F150 + offset);//full width
                case PositionMatchMask.MatchInOtherPosition: return new Rune(0x1F130 + offset);//squared letter
                default: return new Rune(' ');
            }
        }

        string GetMatchSymbol(PositionMatchMask positionMatchMask)
        {
            switch (positionMatchMask)
            {
                case PositionMatchMask.NotMatched: return "⬜";
                case PositionMatchMask.Matched: return "🟩";
                case PositionMatchMask.MatchInOtherPosition: return "🟨";
                default: return string.Empty;
            }
        }
    }

    public static BehaviourStatus SendEndOfGameMessage(this GameContext context)
    {
        var botClient = context.BotClient;
        var wordToGuess = context.Game.GameEngine.GetGameEngineState().WordToGuess;
        botClient.SendTextMessageAsync(chatId: context.ChatId,
                                        text: $"End of Game. Word: {wordToGuess}").Wait();
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