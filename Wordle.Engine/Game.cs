using System.Globalization;
using Microsoft.Azure.CosmosRepository;
using Wordle.Engine.Dictionaries;

namespace Wordle.Engine
{
    public class Game
    {
        private readonly IRepository<GameState> _gameStateRepository;
        private readonly IRepository<PlayerStatistics> _playerStatsRepo;
        public GameEngine GameEngine { get; private set; }

        public Game(IRepository<GameState> gameStateRepository, IRepository<PlayerStatistics> playerStatisticsRepository,
                    IWordsDictionaryService wordsDictionaryService)
        {
            _gameStateRepository = gameStateRepository;
            _playerStatsRepo = playerStatisticsRepository;
            GameEngine = new GameEngine(wordsDictionaryService);
        }

        public async Task SaveInitialPlayerInformationAsync(long chatId, string firstName, string lastName, string userName)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState == null)
            {
                var userDefaultDictionary = WordsDictionaries.GetDefaultDictionaryFromCulture();
                gameState = new GameState()
                {
                    Id = chatId.ToString(),
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = userName,
                    CurrentDictionaryName = userDefaultDictionary.Name
                };
                await _gameStateRepository.CreateAsync(gameState);
            }
            else
            {
                gameState.FirstName = firstName;
                gameState.LastName = lastName;
                gameState.UserName = userName;
                await _gameStateRepository.UpdateAsync(gameState);
            }
        }

        public async Task UpdateDictionaryNameAsync(long chatId, string dictionaryName)
        {
            if(!WordsDictionaries.All.Any(x => x.Name == dictionaryName))
            {
                throw new ArgumentException($"Dictionary {dictionaryName} does not exist");
            }

            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState == null)
            {
                gameState = new GameState()
                {
                    Id = chatId.ToString(),
                    CurrentDictionaryName = dictionaryName
                };
                await _gameStateRepository.CreateAsync(gameState);
            }
            else
            {
                gameState.CurrentDictionaryName = dictionaryName;
                await _gameStateRepository.UpdateAsync(gameState);
            }
        }

        public async Task LoadGameStateAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            var gameEngineState = gameState?.CurrentGameState;
            if (gameEngineState == null)
            {
                var newGameDictionaryName = gameState?.CurrentDictionaryName;
                if(string.IsNullOrEmpty(newGameDictionaryName))
                    newGameDictionaryName = EnglishWordleOriginal.Instance.Name;
                gameEngineState = new GameEngineState(newGameDictionaryName);
            }
            await GameEngine.InitializeGameState(gameEngineState);
        }   

        public async Task SaveGameStateAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            bool exists = true;
            if (gameState == null)
            {
                gameState = new GameState() { Id = chatId.ToString() };
                exists = false;
            }
            var gameEngineState = GameEngine.GetGameEngineState();
            bool shouldSavePlayerStatistics = gameState.CurrentGameState?.CurrentPhase != GamePhase.End && 
                                              gameEngineState.CurrentPhase == GamePhase.End;
            gameState.CurrentGameState = gameEngineState;
            if (exists)
            {
                await _gameStateRepository.UpdateAsync(gameState);
                if(shouldSavePlayerStatistics)
                    await UpdateGameStatisticsAsync(gameState);
            }
            else
                await _gameStateRepository.CreateAsync(gameState);
        }

        public async Task StartNewGameAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState != null)
            {
                gameState.CurrentGameState = null;
                await _gameStateRepository.UpdateAsync(gameState);
            }
        }

        public async Task<PlayerStatisticsWithPositions?> GetPlayerStatisticsAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            var dictionaryName = gameState?.CurrentDictionaryName ?? EnglishWordleOriginal.Instance.Name;
            var playerStatistics = (await _playerStatsRepo.GetAsync(x => x.ChatId == chatId.ToString() &&
                                                                        x.GameMode == "Practice" &&
                                                                        x.DictionaryName == dictionaryName)).FirstOrDefault();
            PlayerStatisticsWithPositions? playerStatisticsWithPositions = null;
            if (playerStatistics != null)
            {
                playerStatisticsWithPositions = new PlayerStatisticsWithPositions(playerStatistics);
                playerStatisticsWithPositions.WinRatePosition = 
                                (await _playerStatsRepo.GetAsync(x => x.ChatId != chatId.ToString() &&
                                                                        x.GameMode == "Practice" &&
                                                                        x.DictionaryName == dictionaryName &&
                                                                        x.WinRate > playerStatistics.WinRate)).Count() + 1;
                playerStatisticsWithPositions.BestStreakPosition = 
                                (await _playerStatsRepo.GetAsync(x => x.ChatId != chatId.ToString() &&
                                                                        x.GameMode == "Practice" &&
                                                                        x.DictionaryName == dictionaryName &&
                                                                        x.BestStreak > playerStatistics.BestStreak)).Count() + 1;
                playerStatisticsWithPositions.PointsPosition = 
                                (await _playerStatsRepo.GetAsync(x => x.ChatId != chatId.ToString() &&
                                                                        x.GameMode == "Practice" &&
                                                                        x.DictionaryName == dictionaryName &&
                                                                        x.AveragePoints > playerStatistics.AveragePoints)).Count() + 1;

            }
            return playerStatisticsWithPositions;
        }

        private async Task UpdateGameStatisticsAsync(GameState gameState)
        {
            var currentGame = gameState.CurrentGameState;
            if (currentGame != null)
            {
                var playerStatistics = (await _playerStatsRepo.GetAsync(x => x.ChatId == gameState.Id &&
                                                                                        x.GameMode == "Practice" &&
                                                                                        x.GameRoomId == gameState.GameRoomId &&
                                                                                        x.DictionaryName == currentGame.DictionaryName &&
                                                                                        x.WordLength == currentGame.WordLength &&
                                                                                        x.MaxAttemptsCount == currentGame.MaxAttemptsCount
                                                                                        )).FirstOrDefault();

                if (playerStatistics == null)
                {
                    playerStatistics = new PlayerStatistics()
                    {
                        ChatId = gameState.Id,
                        GameMode = "Practice",
                        GameRoomId = gameState.GameRoomId,
                        DictionaryName = currentGame.DictionaryName,
                        WordLength = currentGame.WordLength,
                        MaxAttemptsCount = currentGame.MaxAttemptsCount,
                        GamesWonPerAttempt = new int[currentGame.MaxAttemptsCount]
                    };
                    await _playerStatsRepo.CreateAsync(playerStatistics);
                }

                playerStatistics.PlayedGamesCount++;
                if (currentGame.IsWordGuessed)
                {
                    playerStatistics.WonGamesCount++;
                    playerStatistics.GamesWonPerAttempt[currentGame.Attempts.Count - 1]++;
                    playerStatistics.CurrentStreak++;
                    if (playerStatistics.CurrentStreak > playerStatistics.BestStreak)
                        playerStatistics.BestStreak = playerStatistics.CurrentStreak;
                }
                else
                {
                    playerStatistics.CurrentStreak = 0;
                }

                await _playerStatsRepo.UpdateAsync(playerStatistics);
            }
        }
    }
}