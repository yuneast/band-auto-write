namespace BandProgram.Tests;

[Collection("Serial")]
public class PostingFolderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"band-posting-{Guid.NewGuid():N}");
    private readonly string originalCwd = Directory.GetCurrentDirectory();
    private readonly string originalDataDir = AppPaths.DataDir;

    public PostingFolderTests()
    {
        string post1 = Path.Combine(root, "AutoDoc", "Posting", "post_1");
        string post3 = Path.Combine(root, "AutoDoc", "Posting", "post_3");
        Directory.CreateDirectory(post1);
        Directory.CreateDirectory(post3);
        File.WriteAllText(Path.Combine(post1, "contents.txt"), "=가나다콜=\r\n전국 어디서나");
        File.WriteAllText(Path.Combine(post1, "comment_contents.txt"), "댓글입니다");
        File.WriteAllBytes(Path.Combine(post1, "a.png"), new byte[] { 1, 2, 3 });
        File.WriteAllText(Path.Combine(post3, "contents.txt"), "세번째");

        // 기존 코드는 폴더 목록은 현재 디렉터리 기준, 파일 내용은 실행 폴더(startPath) 기준으로 읽는다.
        Directory.SetCurrentDirectory(root);
        AppPaths.DataDir = root;
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(originalCwd);
        AppPaths.DataDir = originalDataDir;
        Directory.Delete(root, true);
    }

    [Fact]
    public void getPostingList_reads_contents_images_and_comment()
    {
        List<Post> posts = new FunctionList().getPostingList("AutoDoc/Posting", "post_");

        Assert.NotNull(posts);
        Post first = posts.Single(p => p.idx == 1);
        Assert.Equal("=가나다콜=" + Environment.NewLine + "전국 어디서나" + Environment.NewLine, first.contents);
        Assert.Single(first.images);
        Assert.Equal("a.png", first.images[0].getFileName());
        Assert.True(first.has_comment);
        Assert.Equal("댓글입니다" + Environment.NewLine, first.comment_contents);

        Post third = posts.Single(p => p.idx == 3);
        Assert.False(third.has_comment);
    }

    [Fact]
    public void getPostingNum_returns_first_gap()
    {
        Assert.Equal(2, new FunctionList().getPostingNum("AutoDoc/Posting", "post_"));
    }
}
