using Xunit;
using System.Collections.Generic;
using Moq;
using Microsoft.Azure.CosmosRepository;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Wordle.Engine.Dictionaries;
using System;

namespace Wordle.Engine.Test;

public class InMemoryWordsDictionaryService_Tests
{
    private readonly InMemoryWordsDictionaryService dictionaryService;

    public InMemoryWordsDictionaryService_Tests()
    {
        dictionaryService = new InMemoryWordsDictionaryService();
    }

    public IWordsDictionary EnglishDictionary { get; set; } = EnglishWordleOriginal.Instance;
    public IWordsDictionary ItalianDictionary { get; set; } = Italian.Instance;

    [Theory]
    [MemberData(nameof(AllDictionaries))]
    public async Task ExistingDictionary_AllWordsValid(IWordsDictionary dictionary)
    {
        foreach(var word in dictionary.Words)
        {
            var result = await dictionaryService.IsWordValid(word, dictionary.Name);
            if(!result)
            {
                throw new Exception($"Word {word} is not valid in dictionary {dictionary.Name}");
            }
            Assert.True(result);
        }
        foreach(var word in dictionary.OtherWords)
        {
            var result = await dictionaryService.IsWordValid(word, dictionary.Name);
            if(!result)
            {
                throw new Exception($"Word {word} is not valid in dictionary {dictionary.Name}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllDictionaries))]
    public async Task ExistingDictionary_PickupWord_ExpectedExistingValue(IWordsDictionary dictionary)
    {
        Assert.True(dictionary.Words.Contains(await dictionaryService.PickWordToGuess(dictionary.Name)));
    }

    [Fact]
    public async Task MissingDictionary_CallIsValidWord_NotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(async () => await dictionaryService.IsWordValid("ANYWORD", "MISSING"));
    }

    [Fact]
    public async Task MissingDictionary_CallPickWordToGuess_NotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(async () => await dictionaryService.PickWordToGuess("MISSING"));
    }

    public static IEnumerable<object[]> AllDictionaries => new [] {
        new object[] { EnglishWordleOriginal.Instance },
        new object[] { Italian.Instance }
    };
}