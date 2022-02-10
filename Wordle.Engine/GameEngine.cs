using System;

namespace Wordle.Engine
{
    public class GameEngine
    {
        private readonly IWordsDictionaryService wordDictionaryService;

        private GamePhase CurrentPhase { get; set; }
        private string WordToGuess { get; set; } = string.Empty;
        private List<string> Attempts { get; set; } = new List<string>();
        private List<PositionMatchMask[]> AttemptsMask { get; set; } = new List<PositionMatchMask[]>();
        private bool IsWordGuessed { get; set; }

        public GameEngine(IWordsDictionaryService wordDictionaryService)
        {
            this.wordDictionaryService = wordDictionaryService;
        }

        public async Task<(WordValidationResult,GameEngineState)> SubmitWord(string word) 
        {
            if(CurrentPhase == GamePhase.Start)
            {
                WordToGuess = await wordDictionaryService.PickWordToGuess();
                CurrentPhase = GamePhase.InProgress;
            }

            var lowerCaseWord = word.ToLowerInvariant();
            var wordEvaluationResult = await ValidateWord(lowerCaseWord);
            if(wordEvaluationResult != WordValidationResult.Accepted)
            {
                return (wordEvaluationResult, GetGameEngineState());
            }
            else
            {
                if (this.CurrentPhase == GamePhase.InProgress)
                {
                    var currentMatch = MatchWord(lowerCaseWord);
                    this.Attempts.Add(lowerCaseWord);
                    this.AttemptsMask.Add(currentMatch);
                    this.IsWordGuessed = currentMatch.All(x => x == PositionMatchMask.Matched);
                    this.CurrentPhase = this.IsWordGuessed || this.Attempts.Count == 5 ? GamePhase.End : GamePhase.InProgress;
                }
                return (wordEvaluationResult, GetGameEngineState());
            }
        }

        private PositionMatchMask[] MatchWord(string word)
        {
            var positionMatch = new PositionMatchMask[5];
            for (int i = 0; i < word.Length; i++)
            {
                if(word[i] == WordToGuess[i])
                {
                    positionMatch[i] = PositionMatchMask.Matched;
                }
                else if(WordToGuess.Contains(word[i]))
                {
                    positionMatch[i] = PositionMatchMask.MatchInOtherPosition;
                }
                else
                {
                    positionMatch[i] = PositionMatchMask.NotMatched;
                }
            }

            return positionMatch;
        }

        private async Task<WordValidationResult> ValidateWord(string word)
        {
            if(word.Length < 5)
            {
                return WordValidationResult.TooShort;
            }
            else if(word.Length > 5)
            {
                return WordValidationResult.TooLong;
            }
            else
            {
                bool isWordInDictionary = await wordDictionaryService.IsWordValid(word);
                if(!isWordInDictionary)
                {
                return WordValidationResult.NotInDictionary;
                }
                else
                {
                    return WordValidationResult.Accepted;
                }
            }
        }

        public async Task InitializeGameState(GameEngineState gameState)
        {
            if(!await ValidateGameState(gameState))
                throw new InvalidOperationException();
            this.Attempts = gameState.Attempts;
            this.AttemptsMask = gameState.AttemptsMask;
            this.CurrentPhase = gameState.CurrentPhase;
            this.IsWordGuessed = gameState.IsWordGuessed;
            this.WordToGuess = gameState.WordToGuess;
        }

        public async Task<bool> ValidateGameState(GameEngineState gameState)
        {
            if(gameState.WordToGuess.Length != 5)
                throw new InvalidOperationException("WordToGuess length must be 5");
            if(!await wordDictionaryService.IsWordValid(gameState.WordToGuess))
                throw new InvalidOperationException("WordToGuess is not within the valid words list");

            return true;
        }

        public GameEngineState GetGameEngineState()
        {
            return new GameEngineState()
            {
                Attempts = this.Attempts,
                AttemptsMask = this.AttemptsMask,
                CurrentPhase = this.CurrentPhase,
                IsWordGuessed = this.IsWordGuessed,
                WordToGuess = this.WordToGuess ?? string.Empty
            };
        } 
       
    }

    public enum WordValidationResult
    {
        NotInDictionary,
        TooShort,
        TooLong,
        Accepted
    }
}