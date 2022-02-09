namespace Wordle.Engine
{
    public class InMemoryWordsDictionaryService : IWordsDictionaryService
    {
        public Task<bool> IsWordValid(string word)
        {
            return Task.FromResult(true);
        }

        public Task<string> PickWordToGuess()
        {
            return Task.FromResult("skill");
        }
    }
}