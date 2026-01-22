using System.Collections.Generic;

namespace BandProgramAvalonia.Models;

public class Post
{
    public int Idx { get; set; }
    public string Contents { get; set; } = string.Empty;
    public List<ImageFile> Images { get; set; } = new();
    public string CommentContents { get; set; } = string.Empty;
    public List<ImageFile> CommentImages { get; set; } = new();
    public bool HasComment { get; set; } = false;
}
