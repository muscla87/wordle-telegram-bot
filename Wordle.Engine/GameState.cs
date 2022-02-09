using Microsoft.Azure.CosmosRepository;

namespace Wordle.Engine
{
    public class GameState : Item
    {
        public GameEngineState? GameEngineState { get; set; }
    }
}