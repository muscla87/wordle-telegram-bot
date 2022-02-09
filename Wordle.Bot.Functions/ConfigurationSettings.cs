using System;

namespace Wordle.Bot.Functions
{
    public static class ConfigurationSettings
    {
        public static string BotApiKey { get { return System.Environment.GetEnvironmentVariable("BOT_TOKEN", EnvironmentVariableTarget.Process);} }
        
    }
}