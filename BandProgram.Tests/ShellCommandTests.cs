using BandProgram.Dev;

namespace BandProgram.Tests;

public class ShellCommandTests
{
    [Fact]
    public void Parses_name_args_and_options()
    {
        ShellCommand cmd = ShellCommand.Parse("search 캠핑 --min 10 --max 500");
        Assert.Equal("search", cmd.Name);
        Assert.Equal(new[] { "캠핑" }, cmd.Args);
        Assert.Equal(10, cmd.IntOption("min", -1));
        Assert.Equal(500, cmd.IntOption("max", -1));
        Assert.Equal(-1, cmd.IntOption("cnt", -1));
    }

    [Fact]
    public void Double_quotes_group_words()
    {
        ShellCommand cmd = ShellCommand.Parse("signup https://band.us/band/123 \"내 닉네임\"");
        Assert.Equal(new[] { "https://band.us/band/123", "내 닉네임" }, cmd.Args);
    }

    [Fact]
    public void Rest_keeps_raw_text_for_selectors()
    {
        ShellCommand cmd = ShellCommand.Parse("sel   [class='cCoverList'] li");
        Assert.Equal("sel", cmd.Name);
        Assert.Equal("[class='cCoverList'] li", cmd.Rest);
    }

    [Fact]
    public void Name_is_lowercased_and_blank_line_is_empty()
    {
        Assert.Equal("post", ShellCommand.Parse("POST").Name);
        Assert.Equal("", ShellCommand.Parse("   ").Name);
    }

    [Fact]
    public void Invalid_number_option_uses_fallback()
    {
        Assert.Equal(7, ShellCommand.Parse("search a --min abc").IntOption("min", 7));
    }
}
