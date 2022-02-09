namespace Wordle.Engine
{
    public interface IWordsDictionaryService
    {
        Task<bool> IsWordValid(string word);
        Task<string> PickWordToGuess();
    }
}