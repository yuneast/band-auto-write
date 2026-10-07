namespace BandProgram.Tests;

[Collection("Serial")]
public class AppPathsTests
{
    [Fact]
    public void DataDirWithSlash_uses_forward_slashes_and_one_trailing_slash()
    {
        string original = AppPaths.DataDir;
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "band-apppaths");
            AppPaths.DataDir = dir + Path.DirectorySeparatorChar;
            Assert.Equal(Path.GetFullPath(dir).Replace('\\', '/').TrimEnd('/') + "/", AppPaths.DataDirWithSlash);
            Assert.False(AppPaths.DataDirWithSlash.EndsWith("//"));
        }
        finally
        {
            AppPaths.DataDir = original;
        }
    }

    [Fact]
    public void DataDir_defaults_to_app_base_directory()
    {
        Assert.Equal(Path.GetFullPath(AppContext.BaseDirectory), Path.GetFullPath(AppPaths.DataDir));
    }
}
