using System.Globalization;

namespace Wordle.Engine.Dictionaries
{
    public static class WordsDictionaries
    {
        public static IWordsDictionary[] All { get; } = new IWordsDictionary[]
        {
            EnglishWordleOriginal.Instance,
            Italian.Instance,
        };
        
        public static IWordsDictionary GetDefaultDictionaryFromCulture()
        {
            switch(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "it":
                    return Italian.Instance;
                default:
                    return EnglishWordleOriginal.Instance;
            }
        }
    }
}