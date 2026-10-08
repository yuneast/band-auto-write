using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace BandProgram.Tests;

public class UpdaterTests : IDisposable
{
    private readonly string app = Path.Combine(Path.GetTempPath(), $"band-updater-{Guid.NewGuid():N}");
    private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly Dictionary<string, byte[]> files = new();
    private readonly HttpListener listener = new();
    private readonly HttpClient http = new();
    private readonly string baseUrl;

    public UpdaterTests()
    {
        Directory.CreateDirectory(app);
        File.WriteAllText(Path.Combine(app, "BandProgram.exe"), "old-exe");
        File.WriteAllText(Path.Combine(app, "selenium-manager.exe"), "old-sm");

        int port = FreePort();
        baseUrl = $"http://localhost:{port}/";
        listener.Prefixes.Add(baseUrl);
        listener.Start();
        _ = Task.Run(Serve);
    }

    public void Dispose()
    {
        listener.Close();
        http.Dispose();
        key.Dispose();
        Directory.Delete(app, true);
    }

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private async Task Serve()
    {
        while (listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); } catch { return; }
            string name = ctx.Request.Url!.AbsolutePath.TrimStart('/');
            if (files.TryGetValue(name, out byte[]? body))
            {
                ctx.Response.ContentLength64 = body.Length;
                await ctx.Response.OutputStream.WriteAsync(body);
            }
            else
            {
                ctx.Response.StatusCode = 404;
            }
            ctx.Response.Close();
        }
    }

    private byte[] Zip(string exe = "new-exe", string sm = "new-sm")
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (n, c) in new[] { ("BandProgram.exe", exe), ("selenium-manager.exe", sm) })
            {
                using Stream s = zip.CreateEntry(n).Open();
                s.Write(Encoding.UTF8.GetBytes(c));
            }
        }
        return ms.ToArray();
    }

    // 서버에 zip, version.json, 서명을 올린다. sha256을 따로 주면 그 값을 manifest에 쓴다(불일치 테스트용).
    private void Publish(string version, byte[] zip, string? sha256 = null, ECDsa? signer = null)
    {
        files["BandProgram-" + version + ".zip"] = zip;
        string sha = sha256 ?? Convert.ToHexString(SHA256.HashData(zip)).ToLowerInvariant();
        byte[] manifest = Encoding.UTF8.GetBytes($$"""{ "version": "{{version}}", "url": "{{baseUrl}}BandProgram-{{version}}.zip", "sha256": "{{sha}}" }""");
        files["version.json"] = manifest;
        files["version.json.sig"] = (signer ?? key).SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
    }

    private Updater NewUpdater(string current = "2026.10.8.900", string? manifestUrl = null) =>
        new(http, new Uri(manifestUrl ?? baseUrl + "version.json"), Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), Version.Parse(current), app);

    private string Read(string name) => File.ReadAllText(Path.Combine(app, name));

    [Fact]
    public void Check_then_install_replaces_files_and_returns_new_exe()
    {
        Publish("2026.10.8.1015", Zip());
        Updater updater = NewUpdater();

        UpdateResult check = updater.Check();
        Assert.Equal(UpdateOutcome.UpdateAvailable, check.Outcome);
        Assert.Equal(new Version(2026, 10, 8, 1015), check.Manifest.Version);

        long last = 0;
        UpdateResult install = updater.Install(check.Manifest, (got, total) => last = got);

        Assert.Equal(UpdateOutcome.ReadyToRestart, install.Outcome);
        Assert.Equal(Path.Combine(app, "BandProgram.exe"), install.NewExePath);
        Assert.Equal("new-exe", Read("BandProgram.exe"));
        Assert.Equal("new-sm", Read("selenium-manager.exe"));
        Assert.Equal("old-exe", Read("BandProgram.old.exe"));
        Assert.True(last > 0);
    }

    [Theory]
    [InlineData("2026.10.8.900")]
    [InlineData("2026.10.8.800")]
    public void Check_reports_no_update_for_same_or_older_version(string serverVersion)
    {
        Publish(serverVersion, Zip());
        Assert.Equal(UpdateOutcome.NoUpdate, NewUpdater("2026.10.8.900").Check().Outcome);
    }

    [Fact]
    public void Check_fails_when_signed_by_another_key()
    {
        using ECDsa other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        Publish("2026.10.8.1015", Zip(), signer: other);
        UpdateResult result = NewUpdater().Check();
        Assert.Equal(UpdateOutcome.Failed, result.Outcome);
        Assert.Contains("서명", result.Reason);
    }

    [Fact]
    public void Check_fails_when_signature_does_not_match_manifest()
    {
        // 업로드 도중: 새 version.json과 옛 서명이 섞인 경우
        Publish("2026.10.8.1000", Zip());
        byte[] oldSig = files["version.json.sig"];
        Publish("2026.10.8.1015", Zip());
        files["version.json.sig"] = oldSig;

        Assert.Equal(UpdateOutcome.Failed, NewUpdater().Check().Outcome);
    }

    [Fact]
    public void Check_fails_when_manifest_missing()
    {
        Assert.Equal(UpdateOutcome.Failed, NewUpdater().Check().Outcome);
    }

    [Fact]
    public void Check_times_out_on_silent_server()
    {
        var silent = new TcpListener(IPAddress.Loopback, 0);
        silent.Start();
        var held = new List<TcpClient>();
        _ = Task.Run(async () => { try { while (true) held.Add(await silent.AcceptTcpClientAsync()); } catch { } });
        try
        {
            int port = ((IPEndPoint)silent.LocalEndpoint).Port;
            Updater updater = NewUpdater(manifestUrl: $"http://127.0.0.1:{port}/version.json");
            updater.ManifestTimeout = TimeSpan.FromSeconds(1);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            UpdateResult result = updater.Check();

            Assert.Equal(UpdateOutcome.Failed, result.Outcome);
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"took {sw.Elapsed}");
        }
        finally
        {
            silent.Stop();
            foreach (TcpClient c in held) c.Dispose();
        }
    }

    [Fact]
    public void Install_fails_on_hash_mismatch_and_leaves_files_untouched()
    {
        Publish("2026.10.8.1015", Zip(), sha256: new string('a', 64));
        Updater updater = NewUpdater();
        UpdateResult check = updater.Check();
        Assert.Equal(UpdateOutcome.UpdateAvailable, check.Outcome);

        UpdateResult install = updater.Install(check.Manifest, null);

        Assert.Equal(UpdateOutcome.Failed, install.Outcome);
        Assert.Contains("해시", install.Reason);
        Assert.Equal("old-exe", Read("BandProgram.exe"));
        Assert.False(File.Exists(Path.Combine(app, "BandProgram.old.exe")));
    }

    [Fact]
    public void Install_reports_permission_error_in_read_only_folder()
    {
        if (OperatingSystem.IsWindows()) return;
        Publish("2026.10.8.1015", Zip());
        Updater updater = NewUpdater();
        UpdateResult check = updater.Check();
        File.SetUnixFileMode(app, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            UpdateResult install = updater.Install(check.Manifest, null);
            Assert.Equal(UpdateOutcome.Failed, install.Outcome);
            Assert.True(install.IsPermissionError);
            Assert.Equal("old-exe", Read("BandProgram.exe"));
        }
        finally
        {
            File.SetUnixFileMode(app, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
