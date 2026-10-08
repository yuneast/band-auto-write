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
