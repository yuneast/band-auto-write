# BandProgram 자동 업데이트 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 고객이 `BandProgram.exe`를 실행하면 서명된 `version.json`으로 새 버전을 확인하고, 있으면 받아서 교체한 뒤 재시작한다. 배포는 `scripts/publish-windows.sh` 하나로 버전 지정, 서명, 업로드까지 한다.

**Architecture:** 확인·검증·다운로드·교체 로직은 `BandProgram.Core/Update/`(UI 없음, 맥에서 테스트)에 두고, WinForms는 진행률 창과 재시작만 맡는다. 서명은 ECDSA P-256/SHA-256(맥 `openssl`로 서명, .NET `ECDsa`로 검증, DER 형식)이다.

**Tech Stack:** .NET 10, WinForms, System.Security.Cryptography.ECDsa, System.IO.Compression, HttpClient, xUnit, bash + openssl + ssh/scp

**Spec:** `docs/superpowers/specs/2026-10-08-auto-update-design.md`

## Global Constraints

- 정책: 강제 업데이트. 서버 버전이 지금 exe 버전보다 **높을 때만** 업데이트한다.
- 서명: ECDSA P-256 + SHA-256, 서명 형식 DER(`DSASignatureFormat.Rfc3279DerSequence`), 공개키는 SubjectPublicKeyInfo DER의 Base64.
- 개인키 경로: `~/.config/band-deploy/update-signing-key.pem`(권한 600, 저장소에 절대 넣지 않음). 환경 변수 `BAND_SIGNING_KEY`로 바꿀 수 있다.
- 서버 주소: `http://newsoft.kr/download/version.json`, `version.json.sig`, `BandProgram-<버전>.zip`, `BandProgram.zip`. 서버 폴더 `/www/band/download/`, 보관 `/root/band-releases/`.
- 배포 파일 이름: `BandProgram.exe`, `selenium-manager.exe`. zip에는 이 두 파일만, 폴더 없이 들어간다.
- 버전 형식: `yyyy.M.d.HHmm` (예: `2026.10.8.1015`, 앞자리 0 없음).
- 시간 제한: `version.json`·서명 요청 각 5초, zip 다운로드 5분.
- 앱 파일: 업데이트 작업 폴더 `.update/`, 옛 파일 `BandProgram.old.exe`, `selenium-manager.old.exe`, 로그 `update.log` — 모두 exe 폴더.
- 업데이트 직후 실행 인자: `--updated` (이 실행은 확인을 건너뛴다).
- 어떤 실패든 지금 버전으로 계속 실행한다. 쓰기 권한 오류만 메시지 상자로 알린다:
  `업데이트하지 못했습니다. 프로그램 폴더의 쓰기 권한을 확인해 주세요.`
- 코드 스타일: 네임스페이스 `BandProgram`(Core·WinForms), Core 파일은 탭 들여쓰기, `Nullable`/`ImplicitUsings` 꺼져 있음(Core·WinForms) — `using`을 명시한다. 테스트 프로젝트는 ImplicitUsings 켜져 있다.
- 명령은 저장소 루트(`/Users/yundongjun/Documents/GitHub/band-auto-write`)에서 실행. 빌드 성공 `오류 0개`, 테스트 성공 `통과!`.
- 커밋 메시지 끝: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`

## Review Focus

1. **업로드 도중 확인한 고객:** `version.json`과 서명이 잠깐 어긋나도 잘못된 업데이트를 받지 않고 지금 버전으로 실행돼야 한다. 테스트: Task 4 `Check_fails_when_signature_does_not_match_manifest`.
2. **교체 중간 실패:** exe는 바꿨는데 `selenium-manager.exe` 교체에서 실패하면 원래 파일로 되돌아가야 한다. 테스트: Task 3 `Swap_failure_in_second_file_restores_everything`.
3. **응답 없는 서버:** 연결은 되는데 응답이 없으면 시간 제한 뒤 포기해야 한다(프로그램이 멈춰 보이면 안 됨). 테스트: Task 4 `Check_times_out_on_silent_server`.
4. **악성 zip:** `..\`나 폴더 경로, 추가 파일이 든 zip은 풀지 않아야 한다. 테스트: Task 3 `TryExtract_rejects_*`.
5. **버전 되돌리기 공격:** 옛 서명 파일을 다시 보내도 낮은 버전으로 바뀌지 않아야 한다. 테스트: Task 4 `Check_reports_no_update_for_same_or_older_version`.

---

## File Structure

```
scripts/create-update-key.sh                     (Task 1) 서명 키 1회 생성
scripts/publish-windows.sh                       (Task 6) 버전·manifest·서명·업로드 순서·검증
BandProgram.Core/Update/UpdatePublicKey.cs       (Task 1) 공개키 상수
BandProgram.Core/Update/UpdateManifest.cs        (Task 2) version.json 해석
BandProgram.Core/Update/ManifestVerifier.cs      (Task 2) 서명 검증
BandProgram.Core/Update/UpdatePackage.cs         (Task 3) zip 검사·풀기, SHA-256
BandProgram.Core/Update/UpdateInstaller.cs       (Task 3) 정리, 교체·되돌리기 (SwapJournal)
BandProgram.Core/Update/Updater.cs               (Task 4) Check / Install 흐름, UpdateResult
BandProgram/UpdateForm.cs                        (Task 5) 진행률 창 (디자이너 파일 없음)
BandProgram/AutoUpdate.cs                        (Task 5) 시작 시 업데이트 실행·재시작
BandProgram/Program.cs                           (Task 5) Main(string[] args), AutoUpdate 호출
BandProgram.Tests/UpdateKeyTests.cs              (Task 1)
BandProgram.Tests/UpdateManifestTests.cs         (Task 2)
BandProgram.Tests/ManifestVerifierTests.cs       (Task 2)
BandProgram.Tests/UpdatePackageTests.cs          (Task 3)
BandProgram.Tests/UpdateInstallerTests.cs        (Task 3)
BandProgram.Tests/UpdaterTests.cs                (Task 4) localhost HTTP 통합 테스트
docs/windows-smoke-test.md, docs/dev-on-mac.md   (Task 6)
```

---

### Task 1: 서명 키 생성 스크립트와 공개키 상수

**Files:**
- Create: `scripts/create-update-key.sh`
- Create: `BandProgram.Core/Update/UpdatePublicKey.cs`
- Test: `BandProgram.Tests/UpdateKeyTests.cs`

**Interfaces:**
- Produces: `UpdatePublicKey.Base64 : string` (internal const), 개인키 파일 `~/.config/band-deploy/update-signing-key.pem`

이 Task는 맥에 실제 운영용 개인키를 만든다. 이미 키가 있으면 스크립트가 멈추므로 덮어쓰지 않는다.

- [ ] **Step 1: 키 생성 스크립트 작성**

`scripts/create-update-key.sh`:

```bash
#!/usr/bin/env bash
# 자동 업데이트 서명 키(ECDSA P-256)를 한 번 만든다.
# 개인키는 이 맥에만 두고(저장소에 넣지 않는다), 출력되는 공개키를
# BandProgram.Core/Update/UpdatePublicKey.cs의 Base64 상수에 넣는다.
#
# 사용법: scripts/create-update-key.sh            새 키 만들기 (이미 있으면 멈춤)
#         scripts/create-update-key.sh --public   기존 키의 공개키만 출력
set -euo pipefail
KEY="${BAND_SIGNING_KEY:-$HOME/.config/band-deploy/update-signing-key.pem}"

if [ "${1:-}" = "--public" ]; then
  openssl pkey -in "$KEY" -pubout -outform DER | base64
  exit 0
fi

if [ -e "$KEY" ]; then
  echo "이미 서명 키가 있습니다: $KEY (덮어쓰지 않습니다)" >&2
  echo "공개키만 보려면: $0 --public" >&2
  exit 1
fi

mkdir -p "$(dirname "$KEY")"
chmod 700 "$(dirname "$KEY")"
(umask 077; openssl ecparam -name prime256v1 -genkey -noout -out "$KEY")
echo "개인키를 만들었습니다: $KEY"
echo "이 파일을 안전한 곳에 백업하세요. 잃어버리면 새 exe를 고객에게 직접 한 번 배포해야 합니다."
echo
echo "공개키 (UpdatePublicKey.cs의 Base64에 넣기):"
openssl pkey -in "$KEY" -pubout -outform DER | base64
```

```bash
chmod +x scripts/create-update-key.sh
```

- [ ] **Step 2: 실패하는 테스트 작성**

`BandProgram.Tests/UpdateKeyTests.cs`:

```csharp
using System.Diagnostics;
using System.Security.Cryptography;

namespace BandProgram.Tests;

public class UpdateKeyTests
{
    [Fact]
    public void Public_key_constant_is_a_valid_P256_key()
    {
        using ECDsa ec = ECDsa.Create();
        ec.ImportSubjectPublicKeyInfo(Convert.FromBase64String(UpdatePublicKey.Base64), out _);
        Assert.Equal(256, ec.KeySize);
    }

    [Fact]
    public void Public_key_constant_matches_local_signing_key_when_present()
    {
        string key = Environment.GetEnvironmentVariable("BAND_SIGNING_KEY")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "band-deploy", "update-signing-key.pem");
        if (!File.Exists(key)) return; // 배포용 맥이 아니면 건너뜀

        var psi = new ProcessStartInfo("openssl") { RedirectStandardOutput = true };
        foreach (string a in new[] { "pkey", "-in", key, "-pubout", "-outform", "DER" }) psi.ArgumentList.Add(a);
        using Process p = Process.Start(psi)!;
        using var ms = new MemoryStream();
        p.StandardOutput.BaseStream.CopyTo(ms);
        p.WaitForExit();

        Assert.Equal(0, p.ExitCode);
        Assert.Equal(Convert.ToBase64String(ms.ToArray()), UpdatePublicKey.Base64);
    }
}
```

Run: `dotnet test BandProgram.Tests --filter UpdateKeyTests 2>&1 | grep -E " error " | head -2`
Expected: `UpdatePublicKey`를 찾을 수 없음

- [ ] **Step 3: 키 만들기**

Run: `scripts/create-update-key.sh`
Expected: `개인키를 만들었습니다: /Users/yundongjun/.config/band-deploy/update-signing-key.pem`와 Base64 공개키 한 줄(약 124자, `MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE`로 시작).
이미 키가 있다고 나오면 `scripts/create-update-key.sh --public`으로 공개키를 얻는다. **개인키 파일 내용은 출력하거나 보고서에 적지 않는다.**

Run: `ls -l ~/.config/band-deploy/update-signing-key.pem`
Expected: `-rw-------`

- [ ] **Step 4: 공개키 상수 작성**

`BandProgram.Core/Update/UpdatePublicKey.cs` (`<공개키>`를 Step 3의 Base64 한 줄로 바꾼다):

```csharp
namespace BandProgram
{
	// 자동 업데이트 서명 검증용 공개키 (ECDSA P-256, SubjectPublicKeyInfo DER의 Base64).
	// scripts/create-update-key.sh가 출력한 값이다. 개인키는 배포하는 맥의
	// ~/.config/band-deploy/update-signing-key.pem에만 있다.
	internal static class UpdatePublicKey
	{
		public const string Base64 = "<공개키>";
	}
}
```

- [ ] **Step 5: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests --filter UpdateKeyTests 2>&1 | tail -1`
Expected: 통과 2, 실패 0

- [ ] **Step 6: Commit**

```bash
git add scripts/create-update-key.sh BandProgram.Core/Update/UpdatePublicKey.cs BandProgram.Tests/UpdateKeyTests.cs
git commit -m "Add update signing key script and embedded public key

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: `version.json` 해석과 서명 검증

**Files:**
- Create: `BandProgram.Core/Update/UpdateManifest.cs`, `BandProgram.Core/Update/ManifestVerifier.cs`
- Test: `BandProgram.Tests/UpdateManifestTests.cs`, `BandProgram.Tests/ManifestVerifierTests.cs`

**Interfaces:**
- Produces:
  - `sealed class UpdateManifest` with `Version Version`, `Uri Url`, `string Sha256` (소문자 64자 16진수), `static bool TryParse(byte[] json, out UpdateManifest manifest, out string error)`
  - `static class ManifestVerifier` with `static bool Verify(byte[] manifest, byte[] signature, string publicKeyBase64)` — 어떤 예외도 던지지 않고 false

- [ ] **Step 1: 실패하는 테스트 작성**

`BandProgram.Tests/UpdateManifestTests.cs`:

```csharp
using System.Text;

namespace BandProgram.Tests;

public class UpdateManifestTests
{
    private const string Sha = "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec";

    private static bool Parse(string json, out UpdateManifest m, out string error)
        => UpdateManifest.TryParse(Encoding.UTF8.GetBytes(json), out m, out error);

    [Fact]
    public void Parses_valid_manifest()
    {
        Assert.True(Parse($$"""{ "version": "2026.10.8.1015", "url": "http://newsoft.kr/download/BandProgram-2026.10.8.1015.zip", "sha256": "{{Sha}}" }""", out var m, out var error), error);
        Assert.Equal(new Version(2026, 10, 8, 1015), m.Version);
        Assert.Equal("http://newsoft.kr/download/BandProgram-2026.10.8.1015.zip", m.Url.ToString());
        Assert.Equal(Sha, m.Sha256);
    }

    [Fact]
    public void Sha256_is_normalized_to_lowercase()
    {
        Assert.True(Parse($$"""{ "version": "1.2", "url": "https://x/a.zip", "sha256": "{{Sha.ToUpperInvariant()}}" }""", out var m, out _));
        Assert.Equal(Sha, m.Sha256);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{ "url": "http://x/a.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "abc", "url": "http://x/a.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "url": "file:///c:/evil.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "url": "relative/a.zip", "sha256": "3062ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    [InlineData("""{ "version": "1.0", "url": "http://x/a.zip", "sha256": "1234" }""")]
    [InlineData("""{ "version": "1.0", "url": "http://x/a.zip", "sha256": "zz62ec4b294ca2304143eb21021ba202b1ec064e861cc1cb9fd45bb8be399cec" }""")]
    public void Rejects_invalid_manifest(string json)
    {
        Assert.False(Parse(json, out var m, out var error));
        Assert.Null(m);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
```

`BandProgram.Tests/ManifestVerifierTests.cs`:

```csharp
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
```

Run: `dotnet test BandProgram.Tests --filter "UpdateManifestTests|ManifestVerifierTests" 2>&1 | grep -E " error " | head -3`
Expected: `UpdateManifest`, `ManifestVerifier`를 찾을 수 없음

- [ ] **Step 2: 구현**

`BandProgram.Core/Update/UpdateManifest.cs`:

```csharp
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BandProgram
{
	// 서버의 version.json. 서명 검증을 통과한 바이트만 해석한다.
	public sealed class UpdateManifest
	{
		private static readonly Regex Sha256Pattern = new Regex("^[0-9a-f]{64}$");

		public Version Version { get; private set; }

		public Uri Url { get; private set; }

		public string Sha256 { get; private set; }

		public static bool TryParse(byte[] json, out UpdateManifest manifest, out string error)
		{
			manifest = null;
			try
			{
				using (JsonDocument doc = JsonDocument.Parse(json))
				{
					JsonElement root = doc.RootElement;
					if (root.ValueKind != JsonValueKind.Object)
					{
						error = "객체가 아님";
						return false;
					}
					Version version;
					if (!TryGetString(root, "version", out string versionText) || !Version.TryParse(versionText, out version))
					{
						error = "version 없음 또는 형식 오류";
						return false;
					}
					Uri url;
					if (!TryGetString(root, "url", out string urlText)
						|| !Uri.TryCreate(urlText, UriKind.Absolute, out url)
						|| (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
					{
						error = "url 없음 또는 http(s)가 아님";
						return false;
					}
					string sha = TryGetString(root, "sha256", out string shaText) ? shaText.ToLowerInvariant() : null;
					if (sha == null || !Sha256Pattern.IsMatch(sha))
					{
						error = "sha256 없음 또는 형식 오류";
						return false;
					}
					manifest = new UpdateManifest { Version = version, Url = url, Sha256 = sha };
					error = null;
					return true;
				}
			}
			catch (JsonException ex)
			{
				error = string.Concat("JSON 오류: ", ex.Message);
				return false;
			}
		}

		private static bool TryGetString(JsonElement root, string name, out string value)
		{
			JsonElement element;
			if (root.TryGetProperty(name, out element) && element.ValueKind == JsonValueKind.String)
			{
				value = element.GetString();
				return true;
			}
			value = null;
			return false;
		}
	}
}
```

`BandProgram.Core/Update/ManifestVerifier.cs`:

```csharp
using System;
using System.Security.Cryptography;

namespace BandProgram
{
	// version.json 서명 검증 (ECDSA P-256 + SHA-256, DER 서명 = openssl dgst -sign 출력 형식).
	public static class ManifestVerifier
	{
		public static bool Verify(byte[] manifest, byte[] signature, string publicKeyBase64)
		{
			if (manifest == null || signature == null || signature.Length == 0 || string.IsNullOrEmpty(publicKeyBase64))
			{
				return false;
			}
			try
			{
				using (ECDsa ecdsa = ECDsa.Create())
				{
					int read;
					ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out read);
					return ecdsa.VerifyData(manifest, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
				}
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
```

- [ ] **Step 3: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests --filter "UpdateManifestTests|ManifestVerifierTests" 2>&1 | tail -1`
Expected: 통과, 실패 0

- [ ] **Step 4: Commit**

```bash
git add BandProgram.Core/Update/UpdateManifest.cs BandProgram.Core/Update/ManifestVerifier.cs BandProgram.Tests/UpdateManifestTests.cs BandProgram.Tests/ManifestVerifierTests.cs
git commit -m "Parse and verify signed update manifests

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: zip 검사·풀기와 파일 교체

**Files:**
- Create: `BandProgram.Core/Update/UpdatePackage.cs`, `BandProgram.Core/Update/UpdateInstaller.cs`
- Test: `BandProgram.Tests/UpdatePackageTests.cs`, `BandProgram.Tests/UpdateInstallerTests.cs`

**Interfaces:**
- Produces:
  - `static class UpdatePackage`: `const string ExeName = "BandProgram.exe"`, `const string SeleniumManagerName = "selenium-manager.exe"`, `static string Sha256Hex(string path)`(소문자), `static bool TryExtract(string zipPath, string targetDir, out string error)`
  - `static class UpdateInstaller`: `const string UpdateDirName = ".update"`, `static string OldName(string fileName)`(`BandProgram.exe` → `BandProgram.old.exe`), `static void Cleanup(string appDir, int attempts = 10, int delayMs = 200)`(예외 없음), `static SwapJournal Swap(string appDir, string newFilesDir)`(실패 시 되돌린 뒤 예외를 다시 던짐)
  - `sealed class SwapJournal`: `bool Undo()`(역순 되돌리기, 예외 없음, 모두 성공하면 true)

- [ ] **Step 1: 실패하는 테스트 작성**

`BandProgram.Tests/UpdatePackageTests.cs`:

```csharp
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace BandProgram.Tests;

public class UpdatePackageTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), $"band-pkg-{Guid.NewGuid():N}");

    public UpdatePackageTests() => Directory.CreateDirectory(dir);

    public void Dispose() => Directory.Delete(dir, true);

    private string MakeZip(params (string name, string content)[] entries)
    {
        string path = Path.Combine(dir, $"{Guid.NewGuid():N}.zip");
        using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using Stream s = zip.CreateEntry(name).Open();
            s.Write(Encoding.UTF8.GetBytes(content));
        }
        return path;
    }

    [Fact]
    public void Sha256Hex_is_lowercase_hex_of_file()
    {
        string file = Path.Combine(dir, "a.bin");
        File.WriteAllBytes(file, new byte[] { 1, 2, 3 });
        Assert.Equal(Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3 })).ToLowerInvariant(), UpdatePackage.Sha256Hex(file));
    }

    [Fact]
    public void TryExtract_accepts_exactly_the_two_files()
    {
        string zip = MakeZip(("BandProgram.exe", "exe"), ("selenium-manager.exe", "sm"));
        string target = Path.Combine(dir, "out");
        Directory.CreateDirectory(target);

        Assert.True(UpdatePackage.TryExtract(zip, target, out string error), error);
        Assert.Equal("exe", File.ReadAllText(Path.Combine(target, "BandProgram.exe")));
        Assert.Equal("sm", File.ReadAllText(Path.Combine(target, "selenium-manager.exe")));
    }

    [Fact]
    public void TryExtract_ignores_name_case_but_writes_canonical_names()
    {
        string zip = MakeZip(("bandprogram.EXE", "exe"), ("Selenium-Manager.exe", "sm"));
        string target = Path.Combine(dir, "out");
        Directory.CreateDirectory(target);

        Assert.True(UpdatePackage.TryExtract(zip, target, out _));
        Assert.Equal(new[] { "BandProgram.exe", "selenium-manager.exe" }, Directory.GetFiles(target).Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("BandProgram.exe")]                                   // selenium-manager 없음
    [InlineData("BandProgram.exe|selenium-manager.exe|extra.dll")]    // 추가 파일
    [InlineData("sub/BandProgram.exe|selenium-manager.exe")]          // 폴더
    [InlineData("..\\BandProgram.exe|selenium-manager.exe")]          // 상위 경로
    [InlineData("BandProgram.exe|BandProgram.exe")]                   // 중복
    public void TryExtract_rejects_unexpected_entries(string names)
    {
        string zip = MakeZip(names.Split('|').Select(n => (n, "x")).ToArray());
        string target = Path.Combine(dir, "out");
        Directory.CreateDirectory(target);

        Assert.False(UpdatePackage.TryExtract(zip, target, out string error));
        Assert.False(string.IsNullOrEmpty(error));
        Assert.Empty(Directory.GetFiles(target));
    }

    [Fact]
    public void TryExtract_rejects_corrupt_zip()
    {
        string bad = Path.Combine(dir, "bad.zip");
        File.WriteAllText(bad, "not a zip");
        Assert.False(UpdatePackage.TryExtract(bad, dir, out _));
    }
}
```

`BandProgram.Tests/UpdateInstallerTests.cs`:

```csharp
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
```

Run: `dotnet test BandProgram.Tests --filter "UpdatePackageTests|UpdateInstallerTests" 2>&1 | grep -E " error " | head -3`
Expected: `UpdatePackage`, `UpdateInstaller`, `SwapJournal`을 찾을 수 없음

- [ ] **Step 2: 구현**

`BandProgram.Core/Update/UpdatePackage.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace BandProgram
{
	// 업데이트 zip: BandProgram.exe와 selenium-manager.exe 두 파일만, 폴더 없이 들어 있어야 한다.
	public static class UpdatePackage
	{
		public const string ExeName = "BandProgram.exe";

		public const string SeleniumManagerName = "selenium-manager.exe";

		private static readonly string[] ExpectedNames = { ExeName, SeleniumManagerName };

		public static string Sha256Hex(string path)
		{
			using (FileStream stream = File.OpenRead(path))
			{
				return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
			}
		}

		public static bool TryExtract(string zipPath, string targetDir, out string error)
		{
			try
			{
				using (ZipArchive zip = ZipFile.OpenRead(zipPath))
				{
					var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						string name = entry.FullName;
						if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || name.Contains("..")
							|| Array.FindIndex(ExpectedNames, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) < 0
							|| entries.ContainsKey(name))
						{
							error = string.Concat("예상하지 못한 항목: ", name);
							return false;
						}
						entries[name] = entry;
					}
					if (entries.Count != ExpectedNames.Length)
					{
						error = "필요한 파일이 없음";
						return false;
					}
					foreach (string name in ExpectedNames)
					{
						entries[name].ExtractToFile(Path.Combine(targetDir, name), true);
					}
				}
				error = null;
				return true;
			}
			catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException)
			{
				error = string.Concat("zip 오류: ", ex.Message);
				return false;
			}
		}
	}
}
```

`BandProgram.Core/Update/UpdateInstaller.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BandProgram
{
	// 실행 중인 exe도 이름 바꾸기는 허용되므로, 옛 파일을 *.old.exe로 옮기고 새 파일을 그 자리에 넣는다.
	public static class UpdateInstaller
	{
		public const string UpdateDirName = ".update";

		private static readonly string[] Files = { UpdatePackage.ExeName, UpdatePackage.SeleniumManagerName };

		public static string OldName(string fileName)
		{
			return string.Concat(Path.GetFileNameWithoutExtension(fileName), ".old", Path.GetExtension(fileName));
		}

		// 지난 업데이트의 잔여물 정리. 이전 프로세스가 종료 중이면 잠겨 있을 수 있어 재시도하고, 끝내 실패하면 다음 실행으로 넘긴다.
		public static void Cleanup(string appDir, int attempts = 10, int delayMs = 200)
		{
			var targets = new List<string>();
			foreach (string file in Files)
			{
				targets.Add(Path.Combine(appDir, OldName(file)));
			}
			string updateDir = Path.Combine(appDir, UpdateDirName);
			for (int i = 0; i < attempts; i++)
			{
				bool done = true;
				foreach (string path in targets)
				{
					done &= TryDelete(() => { if (File.Exists(path)) File.Delete(path); });
				}
				done &= TryDelete(() => { if (Directory.Exists(updateDir)) Directory.Delete(updateDir, true); });
				if (done)
				{
					return;
				}
				Thread.Sleep(delayMs);
			}
		}

		public static SwapJournal Swap(string appDir, string newFilesDir)
		{
			var journal = new SwapJournal();
			try
			{
				foreach (string file in Files)
				{
					string target = Path.Combine(appDir, file);
					string old = Path.Combine(appDir, OldName(file));
					if (File.Exists(old))
					{
						File.Delete(old);
					}
					if (File.Exists(target))
					{
						journal.Move(target, old);
					}
					journal.Move(Path.Combine(newFilesDir, file), target);
				}
				return journal;
			}
			catch
			{
				journal.Undo();
				throw;
			}
		}

		private static bool TryDelete(Action delete)
		{
			try
			{
				delete();
				return true;
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				return false;
			}
		}
	}

	public sealed class SwapJournal
	{
		private readonly List<KeyValuePair<string, string>> moves = new List<KeyValuePair<string, string>>();

		internal void Move(string from, string to)
		{
			File.Move(from, to);
			moves.Add(new KeyValuePair<string, string>(from, to));
		}

		// 한 이름 바꾸기를 역순으로 되돌린다. 하나가 실패해도 나머지는 계속한다.
		public bool Undo()
		{
			bool ok = true;
			for (int i = moves.Count - 1; i >= 0; i--)
			{
				try
				{
					File.Move(moves[i].Value, moves[i].Key);
				}
				catch (Exception)
				{
					ok = false;
				}
			}
			moves.Clear();
			return ok;
		}
	}
}
```

- [ ] **Step 3: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests --filter "UpdatePackageTests|UpdateInstallerTests" 2>&1 | tail -1`
Expected: 통과, 실패 0

- [ ] **Step 4: Commit**

```bash
git add BandProgram.Core/Update/UpdatePackage.cs BandProgram.Core/Update/UpdateInstaller.cs BandProgram.Tests/UpdatePackageTests.cs BandProgram.Tests/UpdateInstallerTests.cs
git commit -m "Validate update zips and swap files with rollback

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: `Updater` 흐름과 localhost 통합 테스트

**Files:**
- Create: `BandProgram.Core/Update/Updater.cs`
- Test: `BandProgram.Tests/UpdaterTests.cs`

**Interfaces:**
- Consumes: Task 2·3의 `UpdateManifest`, `ManifestVerifier`, `UpdatePackage`, `UpdateInstaller`, `SwapJournal`
- Produces:
  - `enum UpdateOutcome { NoUpdate, UpdateAvailable, Failed, ReadyToRestart }`
  - `sealed class UpdateResult`: `UpdateOutcome Outcome`, `string Reason`, `bool IsPermissionError`, `UpdateManifest Manifest`, `SwapJournal Journal`, `string NewExePath`
  - `sealed class Updater`: `static readonly Uri DefaultManifestUrl` (`http://newsoft.kr/download/version.json`),
    ctor `Updater(HttpClient http, Uri manifestUrl, string publicKeyBase64, Version currentVersion, string appDir)`,
    `TimeSpan ManifestTimeout`(기본 5초), `TimeSpan DownloadTimeout`(기본 5분),
    `UpdateResult Check()` → `NoUpdate | UpdateAvailable(Manifest) | Failed`,
    `UpdateResult Install(UpdateManifest manifest, Action<long, long?> progress)` → `ReadyToRestart(Journal, NewExePath) | Failed`
  - 서명 주소는 `manifestUrl + ".sig"`. 어떤 경우에도 예외를 던지지 않는다.

- [ ] **Step 1: 실패하는 테스트 작성**

`BandProgram.Tests/UpdaterTests.cs`:

```csharp
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
```

Run: `dotnet test BandProgram.Tests --filter UpdaterTests 2>&1 | grep -E " error " | head -3`
Expected: `Updater`, `UpdateResult`, `UpdateOutcome`을 찾을 수 없음

- [ ] **Step 2: 구현**

`BandProgram.Core/Update/Updater.cs`:

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace BandProgram
{
	public enum UpdateOutcome
	{
		NoUpdate,
		UpdateAvailable,
		Failed,
		ReadyToRestart
	}

	public sealed class UpdateResult
	{
		public UpdateOutcome Outcome { get; private set; }

		public string Reason { get; private set; }

		public bool IsPermissionError { get; private set; }

		public UpdateManifest Manifest { get; private set; }

		public SwapJournal Journal { get; private set; }

		public string NewExePath { get; private set; }

		internal static UpdateResult NoUpdate(string reason)
		{
			return new UpdateResult { Outcome = UpdateOutcome.NoUpdate, Reason = reason };
		}

		internal static UpdateResult Available(UpdateManifest manifest)
		{
			return new UpdateResult { Outcome = UpdateOutcome.UpdateAvailable, Manifest = manifest, Reason = string.Concat("새 버전 ", manifest.Version) };
		}

		internal static UpdateResult Failed(string reason, bool isPermissionError = false)
		{
			return new UpdateResult { Outcome = UpdateOutcome.Failed, Reason = reason, IsPermissionError = isPermissionError };
		}

		internal static UpdateResult Ready(UpdateManifest manifest, SwapJournal journal, string newExePath)
		{
			return new UpdateResult { Outcome = UpdateOutcome.ReadyToRestart, Manifest = manifest, Journal = journal, NewExePath = newExePath, Reason = string.Concat("업데이트 완료 ", manifest.Version) };
		}
	}

	// 서명된 version.json으로 새 버전을 확인하고, zip을 받아 검증한 뒤 파일을 교체한다.
	// 프로세스 실행과 종료는 호출하는 쪽(WinForms)이 맡는다. 어떤 경우에도 예외를 던지지 않는다.
	public sealed class Updater
	{
		public static readonly Uri DefaultManifestUrl = new Uri("http://newsoft.kr/download/version.json");

		private readonly HttpClient http;
		private readonly Uri manifestUrl;
		private readonly string publicKeyBase64;
		private readonly Version currentVersion;
		private readonly string appDir;

		public Updater(HttpClient http, Uri manifestUrl, string publicKeyBase64, Version currentVersion, string appDir)
		{
			this.http = http;
			this.manifestUrl = manifestUrl;
			this.publicKeyBase64 = publicKeyBase64;
			this.currentVersion = currentVersion;
			this.appDir = appDir;
		}

		public TimeSpan ManifestTimeout { get; set; } = TimeSpan.FromSeconds(5);

		public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(5);

		public UpdateResult Check()
		{
			byte[] manifestBytes;
			byte[] signature;
			try
			{
				manifestBytes = GetBytes(manifestUrl);
				signature = GetBytes(new Uri(string.Concat(manifestUrl.ToString(), ".sig")));
			}
			catch (Exception ex)
			{
				return UpdateResult.Failed(string.Concat("확인 실패: ", ex.Message));
			}
			if (!ManifestVerifier.Verify(manifestBytes, signature, publicKeyBase64))
			{
				return UpdateResult.Failed("서명 검증 실패");
			}
			UpdateManifest manifest;
			string error;
			if (!UpdateManifest.TryParse(manifestBytes, out manifest, out error))
			{
				return UpdateResult.Failed(string.Concat("version.json 오류: ", error));
			}
			if (manifest.Version <= currentVersion)
			{
				return UpdateResult.NoUpdate(string.Concat("최신 버전 사용 중 (", currentVersion, ")"));
			}
			return UpdateResult.Available(manifest);
		}

		public UpdateResult Install(UpdateManifest manifest, Action<long, long?> progress)
		{
			string updateDir = Path.Combine(appDir, UpdateInstaller.UpdateDirName);
			string zipPath = Path.Combine(updateDir, "BandProgram.zip");
			string newDir = Path.Combine(updateDir, "new");
			try
			{
				if (Directory.Exists(updateDir))
				{
					Directory.Delete(updateDir, true);
				}
				Directory.CreateDirectory(newDir);
				Download(manifest.Url, zipPath, progress);
			}
			catch (UnauthorizedAccessException ex)
			{
				return UpdateResult.Failed(string.Concat("다운로드 실패(권한): ", ex.Message), true);
			}
			catch (Exception ex)
			{
				return UpdateResult.Failed(string.Concat("다운로드 실패: ", ex.Message));
			}
			string actual = UpdatePackage.Sha256Hex(zipPath);
			if (!string.Equals(actual, manifest.Sha256, StringComparison.Ordinal))
			{
				return UpdateResult.Failed(string.Concat("해시 불일치 (", actual, ")"));
			}
			string error;
			if (!UpdatePackage.TryExtract(zipPath, newDir, out error))
			{
				return UpdateResult.Failed(error);
			}
			try
			{
				SwapJournal journal = UpdateInstaller.Swap(appDir, newDir);
				return UpdateResult.Ready(manifest, journal, Path.Combine(appDir, UpdatePackage.ExeName));
			}
			catch (UnauthorizedAccessException ex)
			{
				return UpdateResult.Failed(string.Concat("교체 실패(권한): ", ex.Message), true);
			}
			catch (Exception ex)
			{
				return UpdateResult.Failed(string.Concat("교체 실패: ", ex.Message));
			}
		}

		private byte[] GetBytes(Uri url)
		{
			using (var cts = new CancellationTokenSource(ManifestTimeout))
			using (HttpResponseMessage response = http.GetAsync(url, cts.Token).GetAwaiter().GetResult())
			{
				response.EnsureSuccessStatusCode();
				return response.Content.ReadAsByteArrayAsync(cts.Token).GetAwaiter().GetResult();
			}
		}

		private void Download(Uri url, string path, Action<long, long?> progress)
		{
			using (var cts = new CancellationTokenSource(DownloadTimeout))
			using (HttpResponseMessage response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).GetAwaiter().GetResult())
			{
				response.EnsureSuccessStatusCode();
				long? total = response.Content.Headers.ContentLength;
				using (Stream source = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult())
				using (FileStream target = File.Create(path))
				{
					byte[] buffer = new byte[81920];
					long received = 0;
					int read;
					while ((read = source.ReadAsync(buffer, 0, buffer.Length, cts.Token).GetAwaiter().GetResult()) > 0)
					{
						target.Write(buffer, 0, read);
						received += read;
						if (progress != null)
						{
							progress(received, total);
						}
					}
				}
			}
		}
	}
}
```

- [ ] **Step 3: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests --filter UpdaterTests 2>&1 | tail -1`
Expected: 통과, 실패 0

Run: `dotnet test BandProgram.Tests 2>&1 | tail -1`
Expected: 전체 통과

- [ ] **Step 4: Commit**

```bash
git add BandProgram.Core/Update/Updater.cs BandProgram.Tests/UpdaterTests.cs
git commit -m "Add updater flow: check signed manifest, download, verify, swap

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: WinForms 진행률 창과 시작 시 업데이트

**Files:**
- Create: `BandProgram/UpdateForm.cs`, `BandProgram/AutoUpdate.cs`
- Modify: `BandProgram/Program.cs`

**Interfaces:**
- Consumes: Task 1·4의 `UpdatePublicKey.Base64`, `Updater`, `UpdateResult`, `UpdateOutcome`, `UpdateInstaller.Cleanup`
- Produces: `AutoUpdate.Run(string[] args) : bool` (true = 새 버전을 실행했으니 지금 프로세스는 끝낸다)

WinForms는 맥에서 실행할 수 없다. 검증은 빌드와 코드 검토로 하고, 실제 동작은 Task 6의 Windows 체크리스트로 확인한다.

- [ ] **Step 1: 진행률 창**

`BandProgram/UpdateForm.cs`:

```csharp
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BandProgram
{
	// 업데이트 다운로드·설치 동안 띄우는 진행률 창. 닫기 버튼 없이, 작업이 끝나면 스스로 닫힌다.
	internal sealed class UpdateForm : Form
	{
		private readonly Label label = new Label();
		private readonly ProgressBar bar = new ProgressBar();
		private int lastPercent = -1;

		public UpdateForm(Updater updater, UpdateManifest manifest)
		{
			this.Text = "밴드프로그램 업데이트";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.ControlBox = false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.ClientSize = new Size(360, 90);
			this.label.SetBounds(12, 14, 336, 20);
			this.label.Text = string.Concat("새 버전으로 업데이트 중... (", manifest.Version, ")");
			this.bar.SetBounds(12, 46, 336, 22);
			this.bar.Style = ProgressBarStyle.Marquee;
			this.Controls.Add(this.label);
			this.Controls.Add(this.bar);
			this.Shown += (sender, e) =>
			{
				Task.Run(() => updater.Install(manifest, this.ReportProgress)).ContinueWith(task =>
				{
					this.BeginInvoke(new Action(() =>
					{
						this.Result = task.IsFaulted ? null : task.Result;
						this.Close();
					}));
				});
			};
		}

		public UpdateResult Result { get; private set; }

		private void ReportProgress(long received, long? total)
		{
			if (total == null || total.Value <= 0)
			{
				return;
			}
			int percent = (int)(received * 100 / total.Value);
			if (percent == this.lastPercent)
			{
				return;
			}
			this.lastPercent = percent;
			this.BeginInvoke(new Action(() =>
			{
				this.bar.Style = ProgressBarStyle.Continuous;
				this.bar.Value = Math.Min(100, Math.Max(0, percent));
			}));
		}
	}
}
```

- [ ] **Step 2: 시작 시 업데이트 실행**

`BandProgram/AutoUpdate.cs`:

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace BandProgram
{
	// 시작할 때 새 버전을 확인하고, 있으면 묻지 않고 업데이트한 뒤 새 exe를 실행한다(강제 업데이트).
	// 어떤 실패든 지금 버전으로 계속 실행한다. 쓰기 권한 오류만 사용자에게 알린다.
	internal static class AutoUpdate
	{
		private const string UpdatedFlag = "--updated";

		private static readonly string AppDir = AppContext.BaseDirectory;

		// true를 돌려주면 새 버전을 실행했으니 지금 프로세스는 바로 끝내야 한다.
		public static bool Run(string[] args)
		{
			UpdateInstaller.Cleanup(AppDir);
			if (Array.IndexOf(args, UpdatedFlag) >= 0)
			{
				return false;
			}

			UpdateResult result;
			using (var http = new HttpClient())
			{
				Version current = typeof(AutoUpdate).Assembly.GetName().Version;
				var updater = new Updater(http, Updater.DefaultManifestUrl, UpdatePublicKey.Base64, current, AppDir);
				UpdateResult check = updater.Check();
				Log(check.Reason);
				if (check.Outcome != UpdateOutcome.UpdateAvailable)
				{
					return false;
				}
				using (var form = new UpdateForm(updater, check.Manifest))
				{
					Application.Run(form);
					result = form.Result;
				}
			}

			if (result == null || result.Outcome != UpdateOutcome.ReadyToRestart)
			{
				Log(result == null ? "업데이트 중 예기치 않은 오류" : result.Reason);
				if (result != null && result.IsPermissionError)
				{
					MessageBox.Show("업데이트하지 못했습니다. 프로그램 폴더의 쓰기 권한을 확인해 주세요.");
				}
				return false;
			}

			Log(result.Reason);
			try
			{
				var psi = new ProcessStartInfo(result.NewExePath) { UseShellExecute = false, WorkingDirectory = AppDir };
				foreach (string arg in args)
				{
					psi.ArgumentList.Add(arg);
				}
				psi.ArgumentList.Add(UpdatedFlag);
				Process.Start(psi);
				return true;
			}
			catch (Exception ex)
			{
				Log(string.Concat("새 버전 실행 실패, 되돌림: ", ex.Message));
				result.Journal.Undo();
				return false;
			}
		}

		private static void Log(string message)
		{
			try
			{
				File.AppendAllText(Path.Combine(AppDir, "update.log"),
					string.Concat(DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), " ", message, Environment.NewLine));
			}
			catch
			{
			}
		}
	}
}
```

- [ ] **Step 3: `Program.Main` 연결**

`BandProgram/Program.cs`:
1. `static void Main()` → `static void Main(string[] args)`
2. `Directory.SetCurrentDirectory(AppPaths.DataDir);` 바로 다음 줄에 추가(클립보드·SelectorTrace·UTF-8 변환보다 앞):

```csharp
            // 새 버전이 있으면 업데이트하고 새 exe를 실행했으므로 여기서 끝낸다(강제 업데이트).
            if (AutoUpdate.Run(args))
            {
                return;
            }
```

- [ ] **Step 4: 빌드와 전체 테스트**

Run: `dotnet build BandProgram.sln --no-incremental 2>&1 | grep -E "오류 [0-9]+개| error " | sed -E 's/ \[.*\]//' | head -5`
Expected: `오류 0개`

Run: `dotnet test BandProgram.Tests 2>&1 | tail -1`
Expected: 전체 통과

- [ ] **Step 5: Commit**

```bash
git add BandProgram/UpdateForm.cs BandProgram/AutoUpdate.cs BandProgram/Program.cs
git commit -m "Check for updates at startup and restart into the new version

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: 배포 스크립트(버전·서명·업로드 순서·검증)와 문서, 실제 배포

**Files:**
- Modify: `scripts/publish-windows.sh` (전체 교체)
- Modify: `docs/windows-smoke-test.md`, `docs/dev-on-mac.md`

**Interfaces:**
- Consumes: Task 1의 개인키 경로, 서버 레이아웃(Global Constraints)
- Produces: 서버 `/www/band/download/{BandProgram-<버전>.zip, BandProgram.zip, version.json, version.json.sig}`

- [ ] **Step 1: 스크립트 교체**

`scripts/publish-windows.sh` 전체:

```bash
#!/usr/bin/env bash
# 맥에서 고객용 Windows 실행 파일을 만들고, 서명한 업데이트 정보와 함께 서버에 올린다.
#
# 결과 (publish/):
#   win-x64/BandProgram.exe, win-x64/selenium-manager.exe
#   BandProgram-<버전>.zip, BandProgram.zip   고객 배포용 zip (위 두 파일)
#   version.json, version.json.sig           자동 업데이트 정보와 서명
#
# 버전: 실행 시각 yyyy.M.d.HHmm. 서버의 현재 버전보다 높아야 올라간다.
# 업로드: /www/band/download/ (http://newsoft.kr/download/), 보관본 /root/band-releases/
# 서명 키: ~/.config/band-deploy/update-signing-key.pem (scripts/create-update-key.sh로 한 번 만든다)
# 환경 변수: BAND_DEPLOY_HOST, BAND_DEPLOY_PORT, BAND_DEPLOY_USER, BAND_SIGNING_KEY
#
# 사용법: scripts/publish-windows.sh [--no-upload]
set -euo pipefail
cd "$(dirname "$0")/.."

UPLOAD=1
for arg in "$@"; do
  case "$arg" in
    --no-upload) UPLOAD=0 ;;
    *) echo "알 수 없는 옵션: $arg (사용법: $0 [--no-upload])" >&2; exit 2 ;;
  esac
done

DEPLOY_HOST="${BAND_DEPLOY_HOST:-192.168.1.1}"
DEPLOY_PORT="${BAND_DEPLOY_PORT:-47839}"
DEPLOY_USER="${BAND_DEPLOY_USER:-root}"
SIGNING_KEY="${BAND_SIGNING_KEY:-$HOME/.config/band-deploy/update-signing-key.pem}"
WEB_DIR=/www/band/download
RELEASE_DIR=/root/band-releases
BASE_URL=http://newsoft.kr/download

SSH=(ssh -p "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 "$DEPLOY_USER@$DEPLOY_HOST")

# $1이 $2보다 높은 버전이면 참
version_gt() {
  [ "$1" != "$2" ] && [ "$(printf '%s\n%s\n' "$1" "$2" | sort -t. -k1,1n -k2,2n -k3,3n -k4,4n | tail -1)" = "$1" ]
}

if [ "$UPLOAD" -eq 1 ] && [ ! -f "$SIGNING_KEY" ]; then
  echo "서명 키가 없습니다: $SIGNING_KEY" >&2
  echo "처음이면 scripts/create-update-key.sh로 만들고 공개키를 UpdatePublicKey.cs에 넣으세요." >&2
  echo "키를 잃어버렸다면 새 키를 만든 뒤, 새 공개키가 든 exe를 고객에게 직접 한 번 배포해야 합니다." >&2
  exit 1
fi

VERSION=$(date +%Y.%m.%d.%H%M | awk -F. '{ printf "%d.%d.%d.%d", $1, $2, $3, $4 }')
ZIP_NAME="BandProgram-$VERSION.zip"

if [ "$UPLOAD" -eq 1 ]; then
  REMOTE_VERSION=$("${SSH[@]}" "cat $WEB_DIR/version.json 2>/dev/null || true" | sed -n 's/.*"version" *: *"\([0-9.]*\)".*/\1/p')
  if [ -n "$REMOTE_VERSION" ] && ! version_gt "$VERSION" "$REMOTE_VERSION"; then
    echo "서버 버전($REMOTE_VERSION)보다 높지 않아 배포하지 않습니다: $VERSION (1분 뒤 다시 실행하세요)" >&2
    exit 1
  fi
fi

rm -rf publish/win-x64 publish/*.zip publish/version.json publish/version.json.sig
dotnet publish BandProgram/BandProgram.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=embedded \
  -p:Version="$VERSION" \
  -o publish/win-x64
(cd publish/win-x64 && zip -q -X "../$ZIP_NAME" BandProgram.exe selenium-manager.exe)
cp "publish/$ZIP_NAME" publish/BandProgram.zip
SHA=$(shasum -a 256 "publish/$ZIP_NAME" | cut -d' ' -f1)
printf '{\n  "version": "%s",\n  "url": "%s/%s",\n  "sha256": "%s"\n}\n' "$VERSION" "$BASE_URL" "$ZIP_NAME" "$SHA" > publish/version.json
if [ -f "$SIGNING_KEY" ]; then
  openssl dgst -sha256 -sign "$SIGNING_KEY" -out publish/version.json.sig publish/version.json
else
  echo "서명 키가 없어 version.json에 서명하지 않았습니다 (--no-upload)."
fi
echo "빌드 완료: $VERSION (publish/$ZIP_NAME)"

if [ "$UPLOAD" -eq 0 ]; then
  exit 0
fi

upload() {
  scp -q -P "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 "$1" "$DEPLOY_USER@$DEPLOY_HOST:$WEB_DIR/.$2.uploading"
}

echo "업로드 중: $DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PORT"
"${SSH[@]}" "mkdir -p $WEB_DIR $RELEASE_DIR"
upload "publish/$ZIP_NAME" "$ZIP_NAME"
upload publish/version.json version.json
upload publish/version.json.sig version.json.sig

# zip을 먼저 자리에 놓고, 서명 → version.json 순서로 교체한다.
# 그 사이에 확인한 고객은 서명 검증에 실패해 지금 버전으로 실행되고 다음 실행 때 업데이트된다.
REMOTE_SHA=$("${SSH[@]}" "set -e
  cd $WEB_DIR
  chmod 644 .$ZIP_NAME.uploading .version.json.uploading .version.json.sig.uploading
  mv -f .$ZIP_NAME.uploading $ZIP_NAME
  cp $ZIP_NAME .BandProgram.zip.uploading
  mv -f .BandProgram.zip.uploading BandProgram.zip
  cp $ZIP_NAME $RELEASE_DIR/$ZIP_NAME
  mv -f .version.json.sig.uploading version.json.sig
  mv -f .version.json.uploading version.json
  sha256sum $ZIP_NAME | cut -d' ' -f1")
if [ "$SHA" != "$REMOTE_SHA" ]; then
  echo "업로드 확인 실패: 해시가 다릅니다 (로컬 $SHA, 서버 $REMOTE_SHA)" >&2
  exit 1
fi

# 서버가 지금 내려주는 version.json과 서명을 다시 받아 검증한다.
CHECK_DIR=$(mktemp -d)
trap 'rm -rf "$CHECK_DIR"' EXIT
"${SSH[@]}" "cat $WEB_DIR/version.json" > "$CHECK_DIR/version.json"
"${SSH[@]}" "cat $WEB_DIR/version.json.sig" > "$CHECK_DIR/version.json.sig"
openssl pkey -in "$SIGNING_KEY" -pubout -out "$CHECK_DIR/public.pem"
if ! openssl dgst -sha256 -verify "$CHECK_DIR/public.pem" -signature "$CHECK_DIR/version.json.sig" "$CHECK_DIR/version.json" >/dev/null; then
  echo "서버의 version.json 서명 검증 실패" >&2
  exit 1
fi

echo "업로드 완료: $VERSION"
echo "  다운로드:      $BASE_URL/BandProgram.zip"
echo "  업데이트 정보: $BASE_URL/version.json"
echo "  보관본:        $RELEASE_DIR/$ZIP_NAME"
```

Run: `bash -n scripts/publish-windows.sh && echo syntax-ok`
Expected: `syntax-ok`

- [ ] **Step 2: 빌드만 확인 (`--no-upload`)**

Run: `scripts/publish-windows.sh --no-upload 2>&1 | tail -1 && ls publish && cat publish/version.json && openssl dgst -sha256 -verify <(openssl pkey -in ~/.config/band-deploy/update-signing-key.pem -pubout) -signature publish/version.json.sig publish/version.json`
Expected: `빌드 완료: 2026.<M>.<d>.<HHmm> (...)`, `publish/`에 `BandProgram-<버전>.zip`, `BandProgram.zip`, `version.json`, `version.json.sig`, `win-x64`, 그리고 `Verified OK`

Run: `unzip -l publish/BandProgram.zip | tail -4`
Expected: `BandProgram.exe`, `selenium-manager.exe` 두 항목

- [ ] **Step 3: 실제 배포**

Run: `scripts/publish-windows.sh 2>&1 | tail -5`
Expected: `업로드 완료: <버전>`과 주소 세 줄

Run (내부망에서 newsoft.kr 사이트로 요청):
```bash
curl -s -H "Host: newsoft.kr" http://192.168.1.1/download/version.json
curl -s -o /dev/null -w "%{http_code} %{size_download}\n" -H "Host: newsoft.kr" http://192.168.1.1/download/version.json.sig
V=$(curl -s -H "Host: newsoft.kr" http://192.168.1.1/download/version.json | sed -n 's/.*"version" *: *"\([0-9.]*\)".*/\1/p')
curl -s -o /dev/null -w "%{http_code} %{size_download}\n" -H "Host: newsoft.kr" "http://192.168.1.1/download/BandProgram-$V.zip"
```
Expected: 방금 버전의 JSON, 서명 `200 7x`(70~72바이트), zip `200 <약 50000000>`

Run: `scripts/publish-windows.sh 2>&1 | tail -1` (같은 분 안에 다시 실행)
Expected: `서버 버전(...)보다 높지 않아 배포하지 않습니다` — 1분이 지났다면 새 버전으로 배포되며, 이것도 정상이다.

- [ ] **Step 4: 문서**

`docs/windows-smoke-test.md` 표 끝에 추가:

```markdown
| 18 | 자동 업데이트: 업데이트 기능이 든 버전 A 실행 후, 맥에서 `scripts/publish-windows.sh`로 버전 B 배포, A 다시 실행 | "새 버전으로 업데이트 중" 창이 뜨고 B로 재시작된다. exe 속성의 파일 버전이 B다 | |
| 19 | 업데이트 다음 실행 | `BandProgram.old.exe`, `.update` 폴더가 지워져 있다. `update.log`에 기록이 있다 | |
| 20 | 인터넷을 끄고 실행 | 5초 남짓 안에 지금 버전으로 실행된다 | |
| 21 | 쓰기 권한이 없는 폴더(예: `C:\Program Files\BandProgram`)에서 실행 | "업데이트하지 못했습니다… 쓰기 권한…" 안내 후 지금 버전으로 실행된다 | |
```

`docs/dev-on-mac.md` 끝에 추가:

````markdown
## 배포와 자동 업데이트

- 처음 한 번: `scripts/create-update-key.sh`로 서명 키를 만든다(이미 있으면 멈춘다).
  개인키 `~/.config/band-deploy/update-signing-key.pem`은 저장소에 넣지 말고 따로 백업한다.
  잃어버리면 새 키를 만들고 공개키를 `BandProgram.Core/Update/UpdatePublicKey.cs`에 넣은 exe를 고객에게 직접 한 번 배포해야 한다.
- 배포: `scripts/publish-windows.sh` — 버전(실행 시각) 지정, 빌드, zip, `version.json` 서명, 업로드, 서명 재검증까지 한다.
  빌드만 하려면 `--no-upload`.
- 고객 프로그램은 시작할 때 `http://newsoft.kr/download/version.json`을 확인해 새 버전이면 묻지 않고 업데이트한다.
- 문제 있는 배포는 옛 코드로 다시 배포해서 되돌린다(더 높은 버전 번호로 올라간다). `version.json`을 옛 버전으로 바꿔도 고객은 내려가지 않는다.
````

- [ ] **Step 5: Commit**

```bash
git add scripts/publish-windows.sh docs/windows-smoke-test.md docs/dev-on-mac.md
git commit -m "Publish signed update manifests with versioned zips

publish-windows.sh stamps the build with a timestamp version, writes and
signs version.json, uploads versioned zip, signature, then manifest in a
safe order, refuses non-increasing versions, and re-verifies the served
signature.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
