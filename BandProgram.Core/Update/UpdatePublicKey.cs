namespace BandProgram
{
	// 자동 업데이트 서명 검증용 공개키 (ECDSA P-256, SubjectPublicKeyInfo DER의 Base64).
	// scripts/create-update-key.sh가 출력한 값이다. 개인키는 배포하는 맥의
	// ~/.config/band-deploy/update-signing-key.pem에만 있다.
	internal static class UpdatePublicKey
	{
		public const string Base64 = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEX0PRXpalnFyrBC6cwmCbjRxuw70lfXWC3isEDgbeDewLBYDQp6gYVYGQ+5Y+QMeQU4vPtR29OvDrwMsttSbJNw==";
	}
}
