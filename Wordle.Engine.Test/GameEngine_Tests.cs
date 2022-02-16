using Xunit;
using Wordle.Engine;
using System.Linq;
using Moq;
using System.Threading.Tasks;

namespace Wordle.Test;

public class GameEngine_Setup_Tests
{

    public Mock<IWordsDictionaryService> wordsDictionary = new Mock<IWordsDictionaryService>();

    [Fact]
    public void NewGameStateStartsWithSetup()
    {
        var gameEngine = new GameEngine(wordsDictionary.Object);
        Assert.Equal(GamePhase.Start, gameEngine.GetGameEngineState().CurrentPhase);
    }

    
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("aa")]
    [InlineData("aaa")]
    [InlineData("aaaa")]
    public async void SubmitEmptyOrLessThan5CharsWord_Expects_TooShort(string wordToSubmit)
    {
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        Assert.Equal(WordValidationResult.TooShort, wordEvaluationResult);
    }

    [Theory]
    [InlineData("aaaaaaa")]
    [InlineData("aaaaaaaa")]
    [InlineData("aaaaaaaaa")]
    public async void SubmitMoreThan5CharsWord_Expects_TooLong(string wordToSubmit)
    {
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        Assert.Equal(WordValidationResult.TooLong, wordEvaluationResult);
    }

    [Fact]
    public async void SubmitNotInDictionary5CharsWord_Expects_NotInDictionary()
    {
        string wordToSubmit = "aaaaa";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(false));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        Assert.Equal(WordValidationResult.NotInDictionary, wordEvaluationResult);
    }

    [Fact]
    public async void SubmitInDictionary5CharsWord_Expects_Accepted()
    {
        string wordToSubmit = "aaaaa";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToSubmit));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        Assert.Equal(WordValidationResult.Accepted, wordEvaluationResult);
    }

    [Fact]
    public async void SubmitMatchingWord_Expects_GameEnd()
    {
        string wordToSubmit = "aaaaa";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToSubmit));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        Assert.Equal(GamePhase.End, newGameState.CurrentPhase);
        Assert.True(newGameState.IsWordGuessed);
        Assert.True(newGameState.AttemptsMask.Last().All(x => x == PositionMatchMask.Matched));
    }

    [Fact]
    public async void SubmitNotMatchingWord_Expects_Falsy_IsWordGuessed()
    {
        string wordToSubmit = "aaaaa";
        string wordToMatch = "bbbbb";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        Assert.True(newGameState.AttemptsMask.Last().All(x => x == PositionMatchMask.NotMatched));
        Assert.False(newGameState.IsWordGuessed);
    }

    [Fact]
    public async void SubmitPartiallyMatchingWord_Expects_MatchInOtherPositionResult()
    {
        string wordToSubmit = "abcde";
        string wordToMatch = "ecdba";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        System.Diagnostics.Debug.WriteLine(string.Join(",", newGameState.AttemptsMask.Last()));
        Assert.True(newGameState.AttemptsMask.Last().All(x => x == PositionMatchMask.MatchInOtherPosition));
    }

    [Fact]
    public async void SubmitWord_Expects_AttemptsLengthIncrement()
    {
        string wordToSubmit = "aaaaa";
        string wordToMatch = "bbbbb";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        for (int i = 0; i < 5; i++)
        {
            (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
            Assert.Equal(i+1, newGameState.Attempts.Count);
            Assert.Equal(i+1, newGameState.AttemptsMask.Count);
        }
    }

    [Fact]
    public async void SubmitWord5WrongAttempts_Expects_GameEnd()
    {
        string wordToSubmit = "aaaaa";
        string wordToMatch = "bbbbb";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);

        var maxAttempts = gameEngine.GetGameEngineState().MaxAttempts;
        for (int i = 0; i < maxAttempts; i++)
        {
            (var wordEvaluationResult, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);
        }
        var endGameState = gameEngine.GetGameEngineState();
        Assert.False(endGameState.IsWordGuessed);
        Assert.Equal(GamePhase.End, endGameState.CurrentPhase);
    }

    [Fact]
    public async void SubmitWord6Attempts_Expects_NoChangesInState()
    {
        string wordToSubmit = "aaaaa";
        string wordToMatch = "bbbbb";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        var maxAttempts = gameEngine.GetGameEngineState().MaxAttempts;
        for (int i = 0; i < maxAttempts; i++)
        {
            (_, _) = await gameEngine.SubmitWord(wordToSubmit);
        }
        var prevGameState = gameEngine.GetGameEngineState();
        ( _ , var finalGameState) = await gameEngine.SubmitWord(wordToSubmit);

        Assert.Equal(prevGameState.CurrentPhase, finalGameState.CurrentPhase);
        Assert.Equal(prevGameState.Attempts, finalGameState.Attempts);
        Assert.Equal(prevGameState.AttemptsMask, finalGameState.AttemptsMask);
        Assert.Equal(prevGameState.IsWordGuessed, finalGameState.IsWordGuessed);
        Assert.Equal(prevGameState.WordToGuess, finalGameState.WordToGuess);
    }

    [Fact]
    public async void SubmitWordWith2MatchingChars_Expects_OneMatchAndOneNotMatched()
    {
        string wordToSubmit = "knock";
        string wordToMatch = "think";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (_, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);

        var expectedResult = new [] {
            PositionMatchMask.NotMatched,
            PositionMatchMask.MatchInOtherPosition,
            PositionMatchMask.NotMatched,
            PositionMatchMask.NotMatched,
            PositionMatchMask.Matched
        };
        Assert.Equal(expectedResult, newGameState.AttemptsMask[0]);
    }

    [Fact]
    public async void SubmitWordWith2MatchingChars_Expects_OneMatchAndOneMatchInOtherPosition()
    {
        string wordToSubmit = "laaal";
        string wordToMatch = "skill";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (_, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);

        var expectedResult = new [] {
            PositionMatchMask.MatchInOtherPosition,
            PositionMatchMask.NotMatched,
            PositionMatchMask.NotMatched,
            PositionMatchMask.NotMatched,
            PositionMatchMask.Matched
        };
        Assert.Equal(expectedResult, newGameState.AttemptsMask[0]);
    }

    [Fact]
    public async void SubmitWordWith2MatchingChars_Expects__OneMatchInOtherPositionAndOneNotMatch()
    {
        string wordToSubmit = "llbbb";
        string wordToMatch = "aaaal";
        wordsDictionary.Setup(x => x.IsWordValid(wordToSubmit))
                            .Returns(Task.FromResult(true));
        wordsDictionary.Setup(x => x.PickWordToGuess())
                            .Returns(Task.FromResult(wordToMatch));
        var gameEngine = new GameEngine(wordsDictionary.Object);
        (_, var newGameState) = await gameEngine.SubmitWord(wordToSubmit);

        var expectedResult = new [] {
            PositionMatchMask.MatchInOtherPosition,
            PositionMatchMask.NotMatched,
            PositionMatchMask.NotMatched,
            PositionMatchMask.NotMatched,
            PositionMatchMask.NotMatched
        };
        Assert.Equal(expectedResult, newGameState.AttemptsMask[0]);
    }
   
}