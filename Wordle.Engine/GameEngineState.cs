using Microsoft.Azure.CosmosRepository;

namespace Wordle.Engine
{
    public class GameEngineState
    {
        public GameEngineState() { }
        public GameEngineState(string wordToGuess)
        {
            WordToGuess = wordToGuess;
        }

        public GamePhase CurrentPhase { get; set; }
        public string WordToGuess { get; set; } = string.Empty;
        public List<string> Attempts { get; set; } = new List<string>();
        public List<PositionMatchMask[]> AttemptsMask { get; set; } = new List<PositionMatchMask[]>();
        public bool IsWordGuessed { get; set; }
        public int WordLength { get; set; } = 5;
        public int MaxAttempts { get; set; } = 6;
    }

    public enum PositionMatchMask
    {
        NotMatched,
        MatchInOtherPosition,
        Matched
    }
}