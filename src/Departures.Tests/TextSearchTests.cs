using Xunit;

namespace Departures.Tests;

public class TextSearchTests
{
    [Theory]
    [InlineData("Anděl", "andel")]
    [InlineData("Andel", "andel")]
    [InlineData("ANDEL", "andel")]
    [InlineData("Dejvická", "dejvicka")]
    [InlineData("Černý Most", "cerny most")]
    [InlineData("Řepy", "repy")]
    public void Fold_strips_czech_diacritics(string input, string expected)
    {
        Assert.Equal(expected, TextSearch.Fold(input));
    }

    [Fact]
    public void Fold_empty_is_empty()
    {
        Assert.Equal("", TextSearch.Fold(null));
        Assert.Equal("", TextSearch.Fold(""));
    }

    [Theory]
    [InlineData("Anděl", "andel")]
    [InlineData("Anděl", "and")]
    [InlineData("Náměstí Míru", "namesti")]
    public void Matches_ignores_diacritics(string haystack, string needle)
    {
        Assert.True(TextSearch.Matches(haystack, TextSearch.Fold(needle)));
    }

    [Fact]
    public void Matches_rejects_unrelated_text()
    {
        Assert.False(TextSearch.Matches("Anděl", TextSearch.Fold("muzeum")));
    }
}
