namespace Wordle.Engine.Dictionaries
{
    public static class WordsDictionaries
    {
        public static IWordsDictionary[] All { get; } = new IWordsDictionary[]
        {
            EnglishWordleOriginal.Instance,
            Italian.Instance,
        };
    }
}