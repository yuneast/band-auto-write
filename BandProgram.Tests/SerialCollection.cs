namespace BandProgram.Tests;

// 현재 디렉터리, AppPaths, Util 싱글톤 같은 전역 상태를 바꾸는 테스트를 묶어 순서대로 실행한다.
[CollectionDefinition("Serial", DisableParallelization = true)]
public class SerialCollection
{
}
