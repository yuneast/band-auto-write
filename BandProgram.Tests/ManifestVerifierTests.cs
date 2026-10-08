using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace BandProgram.Tests;

public class ManifestVerifierTests
{
    private static readonly byte[] Manifest = Encoding.UTF8.GetBytes("""{ "version": "2026.10.8.1015" }""");

    private static (ECDsa key, string publicBase64) NewKey()
    {
        ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (key, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
    }

    private static byte[] Sign(ECDsa key, byte[] data)
        => key.SignData(data, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

    [Fact]
    public void Accepts_valid_signature()
    {
        var (key, pub) = NewKey();
        Assert.True(ManifestVerifier.Verify(Manifest, Sign(key, Manifest), pub));
    }

    [Fact]
    public void Rejects_modified_manifest()
    {
        var (key, pub) = NewKey();
        byte[] sig = Sign(key, Manifest);
        byte[] tampered = (byte[])Manifest.Clone();
        tampered[5] ^= 1;
        Assert.False(ManifestVerifier.Verify(tampered, sig, pub));
    }

    [Fact]
    public void Rejects_signature_from_another_key()
    {
        var (key, _) = NewKey();
        var (_, otherPub) = NewKey();
        Assert.False(ManifestVerifier.Verify(Manifest, Sign(key, Manifest), otherPub));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1, 2, 3 })]
    public void Rejects_empty_or_garbage_signature(byte[] sig)
    {
        var (_, pub) = NewKey();
        Assert.False(ManifestVerifier.Verify(Manifest, sig, pub));
    }

    [Fact]
    public void Rejects_null_inputs_and_bad_public_key()
    {
        var (key, pub) = NewKey();
        byte[] sig = Sign(key, Manifest);
        Assert.False(ManifestVerifier.Verify(null, sig, pub));
        Assert.False(ManifestVerifier.Verify(Manifest, null, pub));
        Assert.False(ManifestVerifier.Verify(Manifest, sig, "not-base64!"));
    }

    [Fact]
    public void Accepts_signature_made_by_openssl_cli()
    {
        // 배포 스크립트는 openssl로 서명한다. 형식(DER)이 .NET 검증과 맞는지 확인한다.
        string dir = Path.Combine(Path.GetTempPath(), $"band-sig-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            string keyPath = Path.Combine(dir, "key.pem");
            string dataPath = Path.Combine(dir, "version.json");
            string sigPath = Path.Combine(dir, "version.json.sig");
            File.WriteAllBytes(dataPath, Manifest);
            if (!RunOpenssl("ecparam", "-name", "prime256v1", "-genkey", "-noout", "-out", keyPath)) return; // openssl 없음
            Assert.True(RunOpenssl("dgst", "-sha256", "-sign", keyPath, "-out", sigPath, dataPath));
            byte[] spki = RunOpensslBytes("pkey", "-in", keyPath, "-pubout", "-outform", "DER");

            Assert.True(ManifestVerifier.Verify(Manifest, File.ReadAllBytes(sigPath), Convert.ToBase64String(spki)));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static bool RunOpenssl(params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo("openssl") { RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (string a in args) psi.ArgumentList.Add(a);
            using Process p = Process.Start(psi)!;
            p.WaitForExit();
            return p.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static byte[] RunOpensslBytes(params string[] args)
    {
        var psi = new ProcessStartInfo("openssl") { RedirectStandardOutput = true };
        foreach (string a in args) psi.ArgumentList.Add(a);
        using Process p = Process.Start(psi)!;
        using var ms = new MemoryStream();
        p.StandardOutput.BaseStream.CopyTo(ms);
        p.WaitForExit();
        return ms.ToArray();
    }
}
