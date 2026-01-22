namespace BandProgramAvalonia.Models;

public class Band
{
    public string Name { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public string BandKey { get; set; } = string.Empty;

    // UI properties
    public bool IsSelected { get; set; }
    public int Index { get; set; }
    public string Num { get; set; } = string.Empty;
    public string PostingListStr { get; set; } = string.Empty;
    public string CommentListStr { get; set; } = string.Empty;
    public string ChattingListStr { get; set; } = string.Empty;

    public Band() { }

    public Band(string name, string cover, int memberCount, string bandKey)
    {
        Name = name;
        Cover = cover;
        MemberCount = memberCount;
        BandKey = bandKey;
    }
}
