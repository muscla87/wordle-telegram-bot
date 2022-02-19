
namespace Wordle.Engine.Dictionaries;

public interface IWordsDictionary
{

    public static IWordsDictionary? Instance { get; }
    public string Name { get; }

    public string DisplayName { get; }

    /// <summary>
    /// The list that will be used to generate the word to guess.
    /// </summary>
    public string[] Words { get; }
    /// <summary>
    /// Other words that represent valid inputs but won't be used to generate the word to guess.
    /// </summary>
    public string[] OtherWords { get; }

    /// <summary>
    /// The string to format to obtain the url for the word definition.
    /// in example: https://dictionary.cambridge.org/dictionary/english/{0}
    /// </summary>
    public string DefinitionWebSiteUrlFormat { get; }
}
