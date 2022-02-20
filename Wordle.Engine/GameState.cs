using Microsoft.Azure.CosmosRepository;
using Wordle.Engine.Dictionaries;

namespace Wordle.Engine
{
    public class GameState : Item
    {
        public GameEngineState? CurrentGameState { get; set; }
        public string CurrentDictionaryName { get; set; } = EnglishWordleOriginal.Instance.Name;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}