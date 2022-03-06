using Xunit;
using System.Collections.Generic;
using Moq;
using Microsoft.Azure.CosmosRepository;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System;
using System.Linq.Expressions;

namespace Wordle.Engine.Test;

public class Game_Tests
{
    public Mock<IWordsDictionaryService> wordsDictionary = new Mock<IWordsDictionaryService>();
    public Mock<IRepository<GameState>> gameStateRepoMock = new Mock<IRepository<GameState>>();
    public Mock<IRepository<PlayerStatistics>> playerStatisticsRepoMock = new Mock<IRepository<PlayerStatistics>>();
    private readonly Game game;

    public Game_Tests()
    {
        game = new Game(gameStateRepoMock.Object, playerStatisticsRepoMock.Object, wordsDictionary.Object);
    }

    [Fact]
    public async Task LoadGameState_Calls_GetAsync()
    {
        long chatId = 0;
        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
    }

    [Fact]
    public async Task LoadGameState_NoRecordInRepo_GetAsync()
    {
        long chatId = 0;
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(Enumerable.Empty<GameState>()));

        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
    }

    [Fact]
    public async Task LoadGameState_RecordInRepoNoGameEngineState_GetAsync()
    {
        long chatId = 0;

        GameState gameState = new GameState();
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));

        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));

        Assert.Equal(GamePhase.Start, game.GameEngine.GetGameEngineState().CurrentPhase);
    }

    [Fact]
    public async Task LoadGameState_RecordInRepoWithGameEngineState_GetAsync()
    {
        long chatId = 0;

        GameState gameState = new GameState()
        {
            CurrentGameState = new GameEngineState()
                {
                    CurrentPhase = GamePhase.InProgress,
                    Attempts =  new List<string>() { "toll", "skip" },
                    AttemptsMask = new List<PositionMatchMask[]>() {
                        Enumerable.Range(1,5).Select(x => PositionMatchMask.NotMatched).ToArray(),
                        Enumerable.Range(1,5).Select(x => PositionMatchMask.NotMatched).ToArray(),
                    },
                    IsWordGuessed = false,
                    WordToGuess = "zzzzz",
                    DictionaryName = "Italian"
            }
        };
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));
        wordsDictionary.Setup(x => x.IsWordValid(gameState.CurrentGameState.WordToGuess, It.IsAny<string>()))
                                .Returns(Task.FromResult(true));
        await game.LoadGameStateAsync(chatId);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));

        var gameEngineState = game.GameEngine.GetGameEngineState();

        Assert.Equal(gameState.CurrentGameState.CurrentPhase, gameEngineState.CurrentPhase);
        Assert.Equal(gameState.CurrentGameState.Attempts, gameEngineState.Attempts);
        Assert.Equal(gameState.CurrentGameState.AttemptsMask, gameEngineState.AttemptsMask);
        Assert.Equal(gameState.CurrentGameState.IsWordGuessed, gameEngineState.IsWordGuessed);
        Assert.Equal(gameState.CurrentGameState.WordToGuess, gameEngineState.WordToGuess);
        Assert.Equal(gameState.CurrentGameState.DictionaryName, gameEngineState.DictionaryName);
    }

    [Fact]
    public async Task SaveGameState_ShouldTryToLoadExistingRecordAndCheckExistance()
    {
        long chatId = 0;
        await game.SaveGameStateAsync(0);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
    }

    [Fact]
    public async Task SaveGameState_ShouldCallUpdateIfStateExists()
    {
        long chatId = 0;
        GameState gameState = new GameState() { Id = chatId.ToString() };
        gameStateRepoMock.Setup(x => x.ExistsAsync(chatId.ToString(), null, default(CancellationToken)))
                            .Returns(ValueTask.FromResult<bool>(true));
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));

        await game.SaveGameStateAsync(0);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
        gameStateRepoMock.Verify(x => x.UpdateAsync(gameState, default(CancellationToken)));
    }

    [Fact]
    public async Task SaveGameState_ShouldCallCreateIfStateDoesNotExist()
    {
        long chatId = 0;
        gameStateRepoMock.Setup(x => x.ExistsAsync(chatId.ToString(), null, default(CancellationToken)))
                            .Returns(ValueTask.FromResult<bool>(true));
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(Enumerable.Empty<GameState>()));

        await game.SaveGameStateAsync(0);
        gameStateRepoMock.Verify(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)));
        gameStateRepoMock.Verify(x => x.CreateAsync(It.IsAny<GameState>(), default(CancellationToken)));
    }

    [Fact]
    public async Task SaveGameState_GameEnded_ShouldSavePlayerStatistics()
    {
        long chatId = 0;

        GameState gameState = new GameState()
        {
            Id = chatId.ToString(),
            CurrentGameState = new GameEngineState()
                {
                    CurrentPhase = GamePhase.InProgress,
                    Attempts =  new List<string>() { "toll", "skip" },
                    AttemptsMask = new List<PositionMatchMask[]>() {
                        Enumerable.Range(1,5).Select(x => PositionMatchMask.NotMatched).ToArray(),
                        Enumerable.Range(1,5).Select(x => PositionMatchMask.NotMatched).ToArray(),
                    },
                    IsWordGuessed = false,
                    WordToGuess = "zzzzz",
                    DictionaryName = "Italian"
            }
        };
        gameStateRepoMock.Setup(x => x.GetAsync(x => x.Id == chatId.ToString(), default(CancellationToken)))
                            .Returns(ValueTask.FromResult<IEnumerable<GameState>>(new [] { gameState }));
        wordsDictionary.Setup(x => x.IsWordValid(gameState.CurrentGameState.WordToGuess, It.IsAny<string>()))
                                .Returns(Task.FromResult(true));

        await game.LoadGameStateAsync(chatId);
       
        await game.GameEngine.SubmitWord("zzzzz"); //this should end the game and trigger statistics saving
        var gameEngineState = game.GameEngine.GetGameEngineState();

        await game.SaveGameStateAsync(chatId);

        playerStatisticsRepoMock.Verify(x => x.GetAsync( It.IsAny<Expression<Func<PlayerStatistics,bool>>>(), default(CancellationToken)));
        playerStatisticsRepoMock.Verify(x => x.UpdateAsync(It.IsAny<PlayerStatistics>(), default(CancellationToken)));

        //TODO: add more tests to check statistics are properly updated
    }

    [Fact]
    public async Task SaveDictionary_WithNonValidDictionaryName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await game.UpdateDictionaryNameAsync(0, "MISSING"));
    }
}