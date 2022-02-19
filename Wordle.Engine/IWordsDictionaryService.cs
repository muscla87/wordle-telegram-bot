namespace Wordle.Engine
{
    public interface IWordsDictionaryService
    {
        Task<bool> IsWordValid(string word, string dictionaryName);
        Task<string> PickWordToGuess(string dictionaryName);
    }
}