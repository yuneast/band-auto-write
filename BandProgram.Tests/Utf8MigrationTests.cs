using System.Text;

namespace BandProgram.Tests;

public class Utf8MigrationTests : IDisposable
{
    private static readonly Encoding Cp949 = CreateCp949();
    private readonly string root = Path.Combine(Path.GetTempPath(), $"band-migrate-{Guid.NewGuid():N}");

    private static Encoding CreateCp949()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(949);
    }

    public Utf8MigrationTests()
    {
        Directory.CreateDirectory(root);
    }

    public void Dispose()
    {
        Directory.Delete(root, true);
    }

    private string Write(string relative, byte[] bytes)
    {
        string path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public void Converts_cp949_files_and_keeps_backup()
    {
        string bandList = Write("bandList.txt", Cp949.GetBytes("12345\t캠핑 밴드\r\n"));
        string contents = Write(Path.Combine("AutoDoc", "Posting", "post_1", "contents.txt"), Cp949.GetBytes("=가나다콜=\r\n전국"));
        byte[] originalContents = File.ReadAllBytes(contents);

        MigrationResult result = Utf8Migration.Run(root);

        Assert.Empty(result.Failed);
        Assert.Equal(2, result.Converted.Count);
        Assert.Contains("bandList.txt", result.Converted);
        Assert.Equal(Encoding.UTF8.GetBytes("12345\t캠핑 밴드\r\n"), File.ReadAllBytes(bandList));
        Assert.Equal(Encoding.UTF8.GetBytes("=가나다콜=\r\n전국"), File.ReadAllBytes(contents));

        string backup = Path.Combine(root, Utf8Migration.BackupFolder, "AutoDoc", "Posting", "post_1", "contents.txt");
        Assert.Equal(originalContents, File.ReadAllBytes(backup));
    }

    [Fact]
    public void Leaves_utf8_and_ascii_files_untouched()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("이미 UTF-8");
        byte[] ascii = Encoding.ASCII.GetBytes("plain text");
        byte[] withBom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("BOM 있음")).ToArray();
        string a = Write("bandAccount.txt", utf8);
        string b = Write("acc.txt", ascii);
        string c = Write(Path.Combine("AutoDoc", "Comment", "comment_1", "contents.txt"), withBom);

        MigrationResult result = Utf8Migration.Run(root);

        Assert.Empty(result.Converted);
        Assert.Equal(utf8, File.ReadAllBytes(a));
        Assert.Equal(ascii, File.ReadAllBytes(b));
        Assert.Equal(withBom, File.ReadAllBytes(c));
        Assert.False(Directory.Exists(Path.Combine(root, Utf8Migration.BackupFolder)));
    }

    [Fact]
    public void Second_run_changes_nothing()
    {
        Write("bandList.txt", Cp949.GetBytes("낚시"));
        Utf8Migration.Run(root);

        MigrationResult second = Utf8Migration.Run(root);

        Assert.Empty(second.Converted);
        Assert.Empty(second.Failed);
    }

    [Fact]
    public void Only_top_level_txt_and_AutoDoc_are_scanned()
    {
        byte[] cp949 = Cp949.GetBytes("건드리면 안 됨");
        string profile = Write(Path.Combine("chromedata", "Default", "notes.txt"), cp949);
        string backupDir = Write(Path.Combine(Utf8Migration.BackupFolder, "bandList.txt"), cp949);
        string notText = Write("bandList.csv", cp949);

        MigrationResult result = Utf8Migration.Run(root);

        Assert.Empty(result.Converted);
        Assert.Equal(cp949, File.ReadAllBytes(profile));
        Assert.Equal(cp949, File.ReadAllBytes(backupDir));
        Assert.Equal(cp949, File.ReadAllBytes(notText));
    }

    [Fact]
    public void Existing_backup_is_not_overwritten()
    {
        byte[] first = Cp949.GetBytes("처음 원본");
        Write(Path.Combine(Utf8Migration.BackupFolder, "bandList.txt"), first);
        Write("bandList.txt", Cp949.GetBytes("나중 파일"));

        Utf8Migration.Run(root);

        Assert.Equal(first, File.ReadAllBytes(Path.Combine(root, Utf8Migration.BackupFolder, "bandList.txt")));
    }

    [Fact]
    public void Missing_data_dir_returns_empty_result()
    {
        MigrationResult result = Utf8Migration.Run(Path.Combine(root, "missing"));
        Assert.Empty(result.Converted);
        Assert.Empty(result.Failed);
    }

    [Fact]
    public void IsValidUtf8_rejects_cp949_korean()
    {
        Assert.False(Utf8Migration.IsValidUtf8(Cp949.GetBytes("가나다")));
        Assert.True(Utf8Migration.IsValidUtf8(Encoding.UTF8.GetBytes("가나다")));
    }

    [Fact]
    public void Utf16_bom_file_is_left_untouched_without_backup()
    {
        byte[] utf16 = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("유니코드")).ToArray();
        string path = Write(Path.Combine("AutoDoc", "Posting", "post_1", "contents.txt"), utf16);

        MigrationResult result = Utf8Migration.Run(root);

        Assert.Empty(result.Converted);
        Assert.Empty(result.Failed);
        Assert.Equal(utf16, File.ReadAllBytes(path));
        Assert.False(Directory.Exists(Path.Combine(root, Utf8Migration.BackupFolder)));
    }

    [Fact]
    public void Inaccessible_folder_does_not_throw_and_other_files_still_convert()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        Write(Path.Combine("AutoDoc", "locked", "contents.txt"), Cp949.GetBytes("잠긴 폴더"));
        string bandList = Write("bandList.txt", Cp949.GetBytes("12345\t낚시"));
        string locked = Path.Combine(root, "AutoDoc", "locked");
        File.SetUnixFileMode(locked, UnixFileMode.None);
        try
        {
            MigrationResult result = Utf8Migration.Run(root);

            Assert.Contains("bandList.txt", result.Converted);
            Assert.Equal(Encoding.UTF8.GetBytes("12345\t낚시"), File.ReadAllBytes(bandList));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
