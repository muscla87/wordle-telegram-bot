using Wordle.Engine.Dictionaries;

namespace Wordle.Engine
{
    public class InMemoryWordsDictionaryService : IWordsDictionaryService
    {
        static readonly Dictionary<string,(string[], HashSet<string>)> dictionaries = new Dictionary<string, (string[] WordsToGuess, HashSet<string> ValidWords)>();

        static InMemoryWordsDictionaryService()
        {
            foreach (var dictionary in WordsDictionaries.All)
            {
                var hashSet = new HashSet<string>(dictionary.Words.Concat(dictionary.OtherWords), StringComparer.OrdinalIgnoreCase);
                dictionaries.Add(dictionary.Name.ToLowerInvariant(), (dictionary.Words, hashSet));
            }
        }

        Random random = new Random();
        public Task<bool> IsWordValid(string word, string dictionaryName)
        {
            if (!dictionaries.TryGetValue(dictionaryName.ToLowerInvariant(), out (string[] WordsToGuess, HashSet<string> ValidWords) dictionaryInfo))
            {
                throw new NotSupportedException($"Unknown dictionary: {dictionaryName}");
            }
            return Task.FromResult(dictionaryInfo.ValidWords.Contains(word.ToLowerInvariant()));
        }

        public Task<string> PickWordToGuess(string dictionaryName)
        {
            if (!dictionaries.TryGetValue(dictionaryName.ToLowerInvariant(), out (string[] WordsToGuess, HashSet<string> ValidWords) dictionaryInfo))
            {
                throw new NotSupportedException($"Unknown dictionary: {dictionaryName}");
            }
            var randomIndex = random.Next(0, dictionaryInfo.WordsToGuess.Length);
            return Task.FromResult(dictionaryInfo.WordsToGuess[randomIndex]);
        }
    }
}