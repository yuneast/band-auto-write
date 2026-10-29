using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace BandProgram.Tests;

public class ServerTrustTests
{
    // 2026-10-29에 newsoft.kr이 보낸 인증서 (서버 인증서 → YE2 → Root YE → ISRG Root X2 교차 서명). 공개 정보.
    private const string NewsoftChainPem = """
-----BEGIN CERTIFICATE-----
MIIDpTCCAyugAwIBAgISBoe6sZi3mSUl3frlMdzy8lM8MAoGCCqGSM49BAMDMDMx
CzAJBgNVBAYTAlVTMRYwFAYDVQQKEw1MZXQncyBFbmNyeXB0MQwwCgYDVQQDEwNZ
RTIwHhcNMjYxMDI4MDI0MDU2WhcNMjcwMTI2MDI0MDU1WjAVMRMwEQYDVQQDEwpu
ZXdzb2Z0LmtyMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAERI2Z5eRO08a5hNpo
MZIA6/QlwB8NYqvpDKLQSWFWmdiT3z7CfcXpdP/FjjFBBwjmhIW+mUhJRKGdFDut
BzogpKOCAjswggI3MA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAKBggrBgEFBQcD
ATAMBgNVHRMBAf8EAjAAMB0GA1UdDgQWBBQoGFit3xFFVoTPeapyGmPdL2KefDAf
BgNVHSMEGDAWgBS5WfKOzyLwhtM3SP92FBi6gthVhzAzBggrBgEFBQcBAQQnMCUw
IwYIKwYBBQUHMAKGF2h0dHA6Ly95ZTIuaS5sZW5jci5vcmcvMDcGA1UdEQQwMC6C
EGNsb3VkLm5ld3NvZnQua3KCCm5ld3NvZnQua3KCDnd3dy5uZXdzb2Z0LmtyMBMG
A1UdIAQMMAowCAYGZ4EMAQIBMC4GA1UdHwQnMCUwI6AhoB+GHWh0dHA6Ly95ZTIu
Yy5sZW5jci5vcmcvNjEuY3JsMIIBDQYKKwYBBAHWeQIEAgSB/gSB+wD5AHYARMK9
DOkUDmSlyUoBkwpaobs1lw4A7hEWiWgqHETXtWYAAAGhgJdgeQAABAMARzBFAiEA
hd/uzPXlcYZK4ajHV88Zjh/+ohBP31rEWWgY5jqJTrsCIBI2rTPZETqQ/zqhd8q1
jHeXOIzUNG/GyRPzQ7zfE1GUAH8AjspHC6zeavOiBrCkeoS3Rv4fxr+VPiXmm07k
AkjzxugAAAGhgJdhfAAIAAAFADB/MjUEAwBIMEYCIQDb3Dr9kNIM2AFtkw6BXFzC
8ywxWTxTd91WW+Gu43lLCAIhAMbN0cAMcrH8garrFDg/SWdmmUyCBnZYoWaOECCc
XhreMAoGCCqGSM49BAMDA2gAMGUCMATD1qDlZ2i5PuwYogsWyGxhKlf/CerFxDVl
rHCgZs8FJQwltJ6rrfxFK3iMmyco9QIxALVKSPtBvGrOv6fHVqnaWc+x3AESZDGJ
9zTn0z8SopXzPS+PTk+3zQtZNhUNetgdJg==
-----END CERTIFICATE-----
   i:C = US, O = ISRG, CN = Root YE
-----BEGIN CERTIFICATE-----
MIICjDCCAhGgAwIBAgIQTfOxXdbAeExQfNN7WObxFTAKBggqhkjOPQQDAzAuMQsw
CQYDVQQGEwJVUzENMAsGA1UEChMESVNSRzEQMA4GA1UEAxMHUm9vdCBZRTAeFw0y
NTA5MDMwMDAwMDBaFw0yODA5MDIyMzU5NTlaMDMxCzAJBgNVBAYTAlVTMRYwFAYD
VQQKEw1MZXQncyBFbmNyeXB0MQwwCgYDVQQDEwNZRTIwdjAQBgcqhkjOPQIBBgUr
gQQAIgNiAARxmrQzkdbEEL3MqXt3dJQttYc47axkdDTHud5TPqM2z5uSD5cmk0Wr
HlWXvnlvqBLqiB34kluxIbmMyAiq3/YD6e80/vV259K8XQIdjFXloYOa0mIU71f7
HQ09PvYDlw+jge4wgeswDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoGCCsGAQUF
BwMBMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYDVR0OBBYEFLlZ8o7PIvCG0zdI/3YU
GLqC2FWHMB8GA1UdIwQYMBaAFKPIJlqOoUzQNWP8myPIOq5W809WMDIGCCsGAQUF
BwEBBCYwJDAiBggrBgEFBQcwAoYWaHR0cDovL3llLmkubGVuY3Iub3JnLzATBgNV
HSAEDDAKMAgGBmeBDAECATAnBgNVHR8EIDAeMBygGqAYhhZodHRwOi8veWUuYy5s
ZW5jci5vcmcvMAoGCCqGSM49BAMDA2kAMGYCMQDIcnw5dcZLN9ffynXnnkLD/itS
JEycJPb3sRkzeqBowup7vOsAwaqoCnNn/jh9wycCMQCJM6CPlaOC4pQYYbJtVPYb
DKrIb2EKk5NpOpE6/XttQYZV/3gilB9l+Cc/DOVwmyg=
-----END CERTIFICATE-----
   i:C = US, O = Internet Security Research Group, CN = ISRG Root X2
-----BEGIN CERTIFICATE-----
MIICpjCCAiugAwIBAgIRAIchZfw0tuX7qK3Vs3BftTowCgYIKoZIzj0EAwMwTzEL
MAkGA1UEBhMCVVMxKTAnBgNVBAoTIEludGVybmV0IFNlY3VyaXR5IFJlc2VhcmNo
IEdyb3VwMRUwEwYDVQQDEwxJU1JHIFJvb3QgWDIwHhcNMjYwNTEzMDAwMDAwWhcN
MzIwOTAyMjM1OTU5WjAuMQswCQYDVQQGEwJVUzENMAsGA1UEChMESVNSRzEQMA4G
A1UEAxMHUm9vdCBZRTB2MBAGByqGSM49AgEGBSuBBAAiA2IABDwS/6vhrcVqcbBo
+wgdI3fwn9x7DNJJOY/lTOti0vkwuRN87RhEhTH17E7XyFjWsPYhIPt/wzOqxTd2
b+4ZJNy9ID04YywF9U5zasDVyGSNErVNtz8uSGh5izW87j77GaOB6zCB6DAOBgNV
HQ8BAf8EBAMCAQYwEwYDVR0lBAwwCgYIKwYBBQUHAwEwDwYDVR0TAQH/BAUwAwEB
/zAdBgNVHQ4EFgQUo8gmWo6hTNA1Y/ybI8g6rlbzT1YwHwYDVR0jBBgwFoAUfEKW
rt5LSDv6kviejM9ti6lyN5UwMgYIKwYBBQUHAQEEJjAkMCIGCCsGAQUFBzAChhZo
dHRwOi8veDIuaS5sZW5jci5vcmcvMBMGA1UdIAQMMAowCAYGZ4EMAQIBMCcGA1Ud
HwQgMB4wHKAaoBiGFmh0dHA6Ly94Mi5jLmxlbmNyLm9yZy8wCgYIKoZIzj0EAwMD
aQAwZgIxAMU19WCtmxVND8UHBZRoma49Z7jPs64Dma0eTu1OChVbB/2J7GV3nvYK
Ax54uk1G9QIxAO0miLVJu8PLNiXXXkiE/gsK3CTRTF/aeo4bMX42Zw40csRU6AC2
6hSW1/IWaas6dg==
-----END CERTIFICATE-----
   i:C = US, O = Internet Security Research Group, CN = ISRG Root X1
-----BEGIN CERTIFICATE-----
MIIEcDCCAligAwIBAgIQbI8dxyfHEX97r4U6yYD5zTANBgkqhkiG9w0BAQsFADBP
MQswCQYDVQQGEwJVUzEpMCcGA1UEChMgSW50ZXJuZXQgU2VjdXJpdHkgUmVzZWFy
Y2ggR3JvdXAxFTATBgNVBAMTDElTUkcgUm9vdCBYMTAeFw0yNjA1MTMwMDAwMDBa
Fw0zMjA5MDIyMzU5NTlaME8xCzAJBgNVBAYTAlVTMSkwJwYDVQQKEyBJbnRlcm5l
dCBTZWN1cml0eSBSZXNlYXJjaCBHcm91cDEVMBMGA1UEAxMMSVNSRyBSb290IFgy
MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAEzZvVn4CDCuwJSvMWSj5cz3es3mcFDR0H
ttwW+1qLFNvicWDEukWVEYmO6gbf9yoWHKS5xcUy4APgHoIYOIvXRdgKam7mAHf7
AlF9ItgKbppbd9/w+kHsOdx1ymgHDB/qo4H1MIHyMA4GA1UdDwEB/wQEAwIBBjAd
BgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDwYDVR0TAQH/BAUwAwEB/zAd
BgNVHQ4EFgQUfEKWrt5LSDv6kviejM9ti6lyN5UwHwYDVR0jBBgwFoAUebRZ5nu2
5eQBc4AIiMgaWPbpm24wMgYIKwYBBQUHAQEEJjAkMCIGCCsGAQUFBzAChhZodHRw
Oi8veDEuaS5sZW5jci5vcmcvMBMGA1UdIAQMMAowCAYGZ4EMAQIBMCcGA1UdHwQg
MB4wHKAaoBiGFmh0dHA6Ly94MS5jLmxlbmNyLm9yZy8wDQYJKoZIhvcNAQELBQAD
ggIBAD2/e9frmMxNpCV03qUHegg+MV2wz9644YoXdqtH8RyWYcBO7xfjjGEXdU1e
/o0OkEFiynUCOSIk/vLLo7ttz6CPAeNlWfC0XNkoGeWgK6jjXvozBaGuGH5n0Ufo
shMeWTuURqNN5G00sSXDTBrpp2+mgvdZQjb8K11TYMA25QA+YHNfbIEL0BniAhKS
2gsnJjSzrdZLI+EZ7SEyqdR2rkjd1KutLDU+n3TFyxjniZVGur4YlhMP3mY/dV95
IruAkkjOZier6hGBdEgZXXvaCz9u9iVEadsIE75pAGL8oHV5vxdARDiotRpul1IN
/UZwzAbrfUFcw1HkAcYD/mlZfnQ2ieCF2MS7j3Vhv7JPDKp45fmykmzYNSrumRW0
upFFKDBOoF7hsOb7oLyHS+Uft6jOUfOrogj8YUx38hKb2K20r42OgsSdDdxdeYWc
MS3Sb6mwJeSZEYxJ2gaXnDSPaKhhrNkYwljyVQyr4Nq+MEJytXNTnHqaAcrNwZlV
pcJL1KBnMrMjP7eanvUwL3FYj3cF17jtboLt7gLoi4+2rWZFvn+w54jmd/FIuhhZ
cEaU/wvU6BUNMtcVquVGHp7itQeDth5j+XL3j4WJ2SABwzUl6OeYdgpIt/ITZa+p
TT0mQ/r5XyA4MEAiabn7XJjvCERlF2dcn2wqJw+CreTkkQ2R
-----END CERTIFICATE-----
---
Server certificate
subject=CN = newsoft.kr
issuer=C = US, O = Let's Encrypt, CN = YE2
---
No client certificate CA names sent
Peer signing digest: SHA256
Peer signature type: ECDSA
Server Temp Key: X25519, 253 bits
---
SSL handshake has read 3828 bytes and written 396 bytes
Verification: OK
---
New, TLSv1.3, Cipher is TLS_AES_256_GCM_SHA384
Server public key is 256 bit
Secure Renegotiation IS NOT supported
Compression: NONE
Expansion: NONE
No ALPN negotiated
Early data was not sent
Verify return code: 0 (ok)
---
""";

    // 서버 인증서 유효 기간(2026-10-28 ~ 2027-01-26) 안의 시각
    private static readonly DateTime ValidTime = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);

    private static (X509Certificate2 leaf, X509Certificate2Collection extra) NewsoftChain()
    {
        var all = new X509Certificate2Collection();
        all.ImportFromPem(NewsoftChainPem);
        var extra = new X509Certificate2Collection();
        for (int i = 1; i < all.Count; i++)
        {
            extra.Add(all[i]);
        }
        return (all[0], extra);
    }

    [Fact]
    public void Accepts_when_windows_already_trusts_it()
    {
        var (leaf, extra) = NewsoftChain();
        Assert.True(ServerTrust.IsTrusted(leaf, extra, SslPolicyErrors.None, ValidTime));
    }

    [Fact]
    public void Accepts_newsoft_chain_when_windows_lacks_the_root()
    {
        var (leaf, extra) = NewsoftChain();
        Assert.True(ServerTrust.IsTrusted(leaf, extra, SslPolicyErrors.RemoteCertificateChainErrors, ValidTime));
    }

    [Fact]
    public void Rejects_name_mismatch_even_with_valid_chain()
    {
        var (leaf, extra) = NewsoftChain();
        Assert.False(ServerTrust.IsTrusted(leaf, extra, SslPolicyErrors.RemoteCertificateNameMismatch, ValidTime));
        Assert.False(ServerTrust.IsTrusted(leaf, extra,
            SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors, ValidTime));
    }

    [Fact]
    public void Rejects_missing_certificate()
    {
        Assert.False(ServerTrust.IsTrusted(null, new X509Certificate2Collection(), SslPolicyErrors.RemoteCertificateNotAvailable, ValidTime));
    }

    [Fact]
    public void Rejects_expired_certificate()
    {
        var (leaf, extra) = NewsoftChain();
        var afterExpiry = new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.False(ServerTrust.IsTrusted(leaf, extra, SslPolicyErrors.RemoteCertificateChainErrors, afterExpiry));
    }

    [Fact]
    public void Rejects_chain_without_intermediates()
    {
        var (leaf, _) = NewsoftChain();
        Assert.False(ServerTrust.IsTrusted(leaf, new X509Certificate2Collection(), SslPolicyErrors.RemoteCertificateChainErrors, ValidTime));
    }

    [Fact]
    public void Rejects_self_signed_certificate_for_newsoft()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var req = new CertificateRequest("CN=newsoft.kr", key, HashAlgorithmName.SHA256);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("newsoft.kr");
        req.CertificateExtensions.Add(san.Build());
        using var fake = req.CreateSelfSigned(ValidTime.AddDays(-1), ValidTime.AddDays(30));
        Assert.False(ServerTrust.IsTrusted(fake, new X509Certificate2Collection(), SslPolicyErrors.RemoteCertificateChainErrors, ValidTime));
    }

    [Fact]
    public void Bundled_roots_are_the_isrg_roots()
    {
        string[] fingerprints = ServerTrust.Roots().Cast<X509Certificate2>()
            .Select(c => c.GetCertHashString(HashAlgorithmName.SHA256)).ToArray();
        Assert.Equal(new[]
        {
            "96BCEC06264976F37460779ACF28C5A7CFE8A3C0AAE11A8FFCEE05C0BDDF08C6", // ISRG Root X1
            "69729B8E15A86EFC177A57AFB7171DFC64ADD28C2FCA8CF1507E34453CCB1470", // ISRG Root X2
        }, fingerprints);
    }
}
