namespace BandProgram.Tests;

[Collection("Serial")]
public class ParsingTests
{
    [Theory]
    [InlineData("30초", 30)]
    [InlineData("2분", 120)]
    [InlineData("1시간", 3600)]
    [InlineData("무한", 0)]
    public void calculateTime_converts_korean_units_to_seconds(string text, int expected)
    {
        Assert.Equal(expected, Util.getInstance().calculateTime(text));
    }

    [Fact]
    public void stringToIntList_skips_non_numbers()
    {
        Assert.Equal(new List<int> { 1, 2, 3 }, new FunctionList().stringToIntList("1,2,x,3", ','));
    }

    [Fact]
    public void stringToIntList_returns_null_for_null()
    {
        Assert.Null(new FunctionList().stringToIntList(null, ','));
    }

    [Fact]
    public void intListToString_joins_with_comma_space()
    {
        Assert.Equal("1, 2, 3", new FunctionList().intListToString(new List<int> { 1, 2, 3 }));
        Assert.Equal("", new FunctionList().intListToString(null));
    }
}
