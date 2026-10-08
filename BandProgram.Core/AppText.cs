using System.Text;

namespace BandProgram
{
	// 모든 텍스트 파일은 UTF-8(BOM 없음)로 읽고 쓴다.
	// .NET Framework 버전이 CP949(한국어 Windows의 Encoding.Default)로 저장한 파일은
	// 프로그램 시작 시 Utf8Migration이 한 번 변환한다.
	public static class AppText
	{
		public static readonly Encoding Encoding = new UTF8Encoding(false);
	}
}
