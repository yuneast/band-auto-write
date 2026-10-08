namespace BandProgram.Dev;

internal static class Program
{
    private static int Main(string[] args)
    {
        string dataDir = "devdata";
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--data") dataDir = args[i + 1];
        }
        dataDir = Path.GetFullPath(dataDir);
        Directory.CreateDirectory(dataDir);

        // 기존 코드는 상대 경로 파일(bandList.txt 등)을 현재 디렉터리 기준으로 읽는다.
        AppPaths.DataDir = dataDir;
        Directory.SetCurrentDirectory(dataDir);

        SelectorTrace.SnapshotEnabled = true;
        SelectorTrace.Sink = line => DevShell.Log(line, ConsoleColor.Yellow);

        // Windows에서 복사해 온 CP949 파일을 UTF-8로 바꾼다(고객 앱과 같은 처리).
        MigrationResult migration = Utf8Migration.Run(dataDir);
        foreach (string file in migration.Converted) Console.WriteLine($"UTF-8로 변환: {file}");
        foreach (string failure in migration.Failed) Console.WriteLine($"변환 실패: {failure}");

        Console.WriteLine($"데이터 폴더: {dataDir}");
        Console.WriteLine("help 로 명령 목록을 볼 수 있어요.");

        var shell = new DevShell();
        while (true)
        {
            Console.Write("band> ");
            string line = Console.ReadLine();
            if (line == null || !shell.Execute(line)) break;
        }
        return 0;
    }
}
