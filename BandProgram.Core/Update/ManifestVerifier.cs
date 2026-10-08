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
