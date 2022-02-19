using Microsoft.Azure.CosmosRepository;
using Wordle.Engine.Dictionaries;

namespace Wordle.Engine
{
    public class GameState : Item
    {
        public GameEngineState? CurrentGameState { get; set; }
        public string CurrentDictionary { get; set; } = EnglishWordleOriginal.Instance.Name;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}