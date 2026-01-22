using System.Collections.Generic;

namespace BandProgramAvalonia.Models;

public class BandInfo
{
    public string Name { get; set; } = string.Empty;
    public string Num { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<int> PostingList { get; set; } = new();
    public List<int> CommentList { get; set; } = new();
    public List<int> ChattingList { get; set; } = new();
}
