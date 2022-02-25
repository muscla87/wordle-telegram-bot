using System;
using BehaviourTree;
using Microsoft.Extensions.Localization;
using Telegram.Bot;
using Wordle.Bot.Functions.Resources;
using Wordle.Engine;

namespace Wordle.Bot;

public class GameContext
{
    public long ChatId { get; set; }
    public int MessageId { get; set; }
    public string Message { get; set; }
    public Game Game { get; set; }
    public string PlayerFirstName { get; set; }
    public string PlayerLastName { get; set; }
    public string PlayerUserName { get; set; }

    //Services
    public TelegramBotClient BotClient { get; set; }
    public IStringLocalizer<Messages> Localizer { get; set; }
}