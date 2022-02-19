using System;
using Wordle.Engine.Dictionaries;

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
        private int WordLength { get; set; } = 5;
        private int MaxAttempts { get; set; } = 6;
        private string DictionaryName { get; set; } = EnglishWordleOriginal.Instance.Name;


        public GameEngine(IWordsDictionaryService wordDictionaryService)
        {
            this.wordDictionaryService = wordDictionaryService;
        }

        public async Task<(WordValidationResult,GameEngineState)> SubmitWord(string word) 
        {
            if(CurrentPhase == GamePhase.Start)
            {
                WordToGuess = await wordDictionaryService.PickWordToGuess(DictionaryName);
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
                    this.CurrentPhase = this.IsWordGuessed || this.Attempts.Count == this.MaxAttempts ? GamePhase.End : GamePhase.InProgress;
                }
                return (wordEvaluationResult, GetGameEngineState());
            }
        }

        private PositionMatchMask[] MatchWord(string word)
        {
            const char replacementChar = '*';
            var positionMatch = new PositionMatchMask[this.WordLength];
            var tmpWordToGuess = WordToGuess.ToLowerInvariant();
            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == tmpWordToGuess[i])
                {
                    positionMatch[i] = PositionMatchMask.Matched;
                    tmpWordToGuess = tmpWordToGuess.Remove(i, 1).Insert(i, replacementChar.ToString());
                }
            }
            var countByChars = tmpWordToGuess.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            for (int i = 0; i < word.Length; i++)
            {
                if(tmpWordToGuess[i] == replacementChar)
                    continue;

                if(word[i] == tmpWordToGuess[i])
                {
                    positionMatch[i] = PositionMatchMask.Matched;
                }
                else if(tmpWordToGuess.Contains(word[i]))
                {
                    if(countByChars[word[i]] > 0)
                    {
                        positionMatch[i] = PositionMatchMask.MatchInOtherPosition;
                        countByChars[word[i]]--;
                    }
                    else
                    {
                        positionMatch[i] = PositionMatchMask.NotMatched;
                    }
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
            if(word.Length < this.WordLength)
            {
                return WordValidationResult.TooShort;
            }
            else if(word.Length > this.WordLength)
            {
                return WordValidationResult.TooLong;
            }
            else
            {
                bool isWordInDictionary = await wordDictionaryService.IsWordValid(word, DictionaryName);
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
            this.WordLength = gameState.WordLength;
            this.MaxAttempts = gameState.MaxAttempts;
            this.DictionaryName = gameState.DictionaryName;
        }

        public async Task<bool> ValidateGameState(GameEngineState gameState)
        {
            if (CurrentPhase != GamePhase.Start)
            {
                if (gameState.WordToGuess.Length != gameState.WordLength)
                    throw new InvalidOperationException($"WordToGuess length must be {gameState.WordLength} if game is started");
                if (!await wordDictionaryService.IsWordValid(gameState.WordToGuess, DictionaryName))
                    throw new InvalidOperationException("WordToGuess is not within the valid words list");
            }
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
                WordToGuess = this.WordToGuess ?? string.Empty,
                WordLength = this.WordLength,
                MaxAttempts = this.MaxAttempts,
                DictionaryName = this.DictionaryName
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