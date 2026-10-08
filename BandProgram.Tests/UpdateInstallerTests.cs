namespace BandProgram.Tests;

public class UpdateInstallerTests : IDisposable
{
    private readonly string app = Path.Combine(Path.GetTempPath(), $"band-app-{Guid.NewGuid():N}");
    private readonly string fresh;

    public UpdateInstallerTests()
    {
        fresh = Path.Combine(app, ".update", "new");
        Directory.CreateDirectory(fresh);
        File.WriteAllText(Path.Combine(app, "BandProgram.exe"), "old-exe");
        File.WriteAllText(Path.Combine(app, "selenium-manager.exe"), "old-sm");
        File.WriteAllText(Path.Combine(app, "bandList.txt"), "data");
        File.WriteAllText(Path.Combine(fresh, "BandProgram.exe"), "new-exe");
        File.WriteAllText(Path.Combine(fresh, "selenium-manager.exe"), "new-sm");
    }

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(app, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Directory.Delete(app, true);
    }

    private string Read(string name) => File.ReadAllText(Path.Combine(app, name));

    [Fact]
    public void OldName_inserts_old_before_extension()
    {
        Assert.Equal("BandProgram.old.exe", UpdateInstaller.OldName("BandProgram.exe"));
        Assert.Equal("selenium-manager.old.exe", UpdateInstaller.OldName("selenium-manager.exe"));
    }

    [Fact]
    public void Swap_puts_new_files_in_place_and_keeps_old_ones()
    {
        UpdateInstaller.Swap(app, fresh);

        Assert.Equal("new-exe", Read("BandProgram.exe"));
        Assert.Equal("new-sm", Read("selenium-manager.exe"));
        Assert.Equal("old-exe", Read("BandProgram.old.exe"));
        Assert.Equal("old-sm", Read("selenium-manager.old.exe"));
        Assert.Equal("data", Read("bandList.txt"));
    }

    [Fact]
    public void Swap_works_when_selenium_manager_was_missing()
    {
        File.Delete(Path.Combine(app, "selenium-manager.exe"));
        UpdateInstaller.Swap(app, fresh);
        Assert.Equal("new-sm", Read("selenium-manager.exe"));
    }

    [Fact]
    public void Swap_failure_in_second_file_restores_everything()
    {
        File.Delete(Path.Combine(fresh, "selenium-manager.exe")); // 두 번째 단계에서 실패

        Assert.ThrowsAny<IOException>(() => UpdateInstaller.Swap(app, fresh));

        Assert.Equal("old-exe", Read("BandProgram.exe"));
        Assert.Equal("old-sm", Read("selenium-manager.exe"));
        Assert.False(File.Exists(Path.Combine(app, "BandProgram.old.exe")));
        Assert.False(File.Exists(Path.Combine(app, "selenium-manager.old.exe")));
        Assert.Equal("new-exe", File.ReadAllText(Path.Combine(fresh, "BandProgram.exe")));
    }

    [Fact]
    public void Journal_undo_after_success_restores_old_files()
    {
        SwapJournal journal = UpdateInstaller.Swap(app, fresh);

        Assert.True(journal.Undo());

        Assert.Equal("old-exe", Read("BandProgram.exe"));
        Assert.Equal("old-sm", Read("selenium-manager.exe"));
        Assert.False(File.Exists(Path.Combine(app, "BandProgram.old.exe")));
    }

    [Fact]
    public void Swap_in_read_only_folder_throws_permission_error_and_changes_nothing()
    {
        if (OperatingSystem.IsWindows()) return;
        File.SetUnixFileMode(app, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        Assert.Throws<UnauthorizedAccessException>(() => UpdateInstaller.Swap(app, fresh));
        Assert.Equal("old-exe", Read("BandProgram.exe"));
    }

    [Fact]
    public void Cleanup_removes_old_files_and_update_folder_only()
    {
        UpdateInstaller.Swap(app, fresh);

        UpdateInstaller.Cleanup(app);

        Assert.False(File.Exists(Path.Combine(app, "BandProgram.old.exe")));
        Assert.False(File.Exists(Path.Combine(app, "selenium-manager.old.exe")));
        Assert.False(Directory.Exists(Path.Combine(app, ".update")));
        Assert.Equal("new-exe", Read("BandProgram.exe"));
        Assert.Equal("data", Read("bandList.txt"));
    }

    [Fact]
    public void Cleanup_gives_up_quietly_when_files_cannot_be_deleted()
    {
        if (OperatingSystem.IsWindows()) return;
        UpdateInstaller.Swap(app, fresh);
        File.SetUnixFileMode(app, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        UpdateInstaller.Cleanup(app, attempts: 2, delayMs: 1); // 예외 없이 끝나야 한다

        Assert.True(File.Exists(Path.Combine(app, "BandProgram.old.exe")));
    }
}
