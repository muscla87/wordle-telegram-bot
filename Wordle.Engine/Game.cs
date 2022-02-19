using Microsoft.Azure.CosmosRepository;
using Wordle.Engine.Dictionaries;

namespace Wordle.Engine
{
    public class Game
    {
        private readonly IRepository<GameState> _gameStateRepository;
        public GameEngine GameEngine { get; private set; }

        public Game(IRepository<GameState> gameStateRepository, IWordsDictionaryService wordsDictionaryService)
        {
            _gameStateRepository = gameStateRepository;
            GameEngine = new GameEngine(wordsDictionaryService);
        }

        public async Task SaveInitialPlayerInformation(long chatId, string fullName, string userName)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState == null)
            {
                gameState = new GameState()
                {
                    Id = chatId.ToString(),
                    FullName = fullName,
                    UserName = userName
                };
                await _gameStateRepository.CreateAsync(gameState);
            }
            else
            {
                gameState.FullName = fullName;
                gameState.UserName = userName;
                await _gameStateRepository.UpdateAsync(gameState);
            }
        }

        public async Task SaveDictionaryName(long chatId, string dictionaryName)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState == null)
            {
                gameState = new GameState()
                {
                    Id = chatId.ToString(),
                    CurrentDictionary = dictionaryName
                };
                await _gameStateRepository.CreateAsync(gameState);
            }
            else
            {
                gameState.CurrentDictionary = dictionaryName;
                await _gameStateRepository.UpdateAsync(gameState);
            }
        }

        public async Task LoadGameStateAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            var gameEngineState = gameState?.CurrentGameState;
            if (gameEngineState == null)
            {
                var newGameDictionaryName = gameState?.CurrentDictionary;
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
            gameState.CurrentGameState = gameEngineState;
            if(exists)
                await _gameStateRepository.UpdateAsync(gameState);
            else
                await _gameStateRepository.CreateAsync(gameState);
        }

        public async Task DeleteGameStateAsync(long chatId)
        {
            var gameState = (await _gameStateRepository.GetAsync(x => x.Id == chatId.ToString())).FirstOrDefault();
            if (gameState != null)
            {
                await _gameStateRepository.DeleteAsync(gameState);
            }
        }
    }
}