using OpenQA.Selenium;

namespace BandProgram.Dev;

internal sealed class DevShell
{
    private static readonly object ConsoleLock = new();

    private readonly FunctionList fl = new();
    private readonly Util util = Util.getInstance();
    private readonly WorkControl work = new();

    public static void Log(string message, ConsoleColor? color = null)
    {
        lock (ConsoleLock)
        {
            if (color.HasValue) Console.ForegroundColor = color.Value;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} {message}");
            if (color.HasValue) Console.ResetColor();
        }
    }

    // false를 돌려주면 셸을 끝낸다.
    public bool Execute(string line)
    {
        ShellCommand cmd = ShellCommand.Parse(line);
        try
        {
            switch (cmd.Name)
            {
                case "": return true;
                case "help": PrintHelp(); return true;
                case "account": Account(cmd); return true;
                case "login": Login(cmd); return true;
                case "chrome": Log(fl.startChrome(0) ? "Chrome 실행됨" : "Chrome 실행 실패"); return true;
                case "bands": Bands(); return true;
                case "search": Search(cmd); return true;
                case "signup": Signup(cmd); return true;
                case "post": StartJob("포스팅", c => fl.setPostingParam(c.Type, c.BetweenWorkSec, c.Reserved, c.Paste, c.ReserveHour, c.ReserveMin, c.RepeatCount, c.RepeatBetweenSec), LoadConfig().Posting, () => fl.startPosting(PrintLog, PrintLogLeft)); return true;
                case "comment": StartJob("댓글", c => fl.setCommentParam(c.Type, c.BetweenWorkSec, c.Reserved, c.Paste, c.ReserveHour, c.ReserveMin, c.RepeatCount, c.RepeatBetweenSec), LoadConfig().Comment, () => fl.startComment(PrintLog, PrintLogLeft)); return true;
                case "chat": StartJob("채팅", c => fl.setChattingParam(c.Type, c.BetweenWorkSec, c.Reserved, c.Paste, c.ReserveHour, c.ReserveMin, c.RepeatCount, c.RepeatBetweenSec), LoadConfig().Chatting, () => fl.startChatting(PrintLog, PrintLogLeft)); return true;
                case "pause": work.Pause(); Log("일시정지 (다음 delay 지점에서 멈춤)"); return true;
                case "resume": work.Resume(); Log("재개"); return true;
                case "init": work.Reset(); Log("초기화 (진행 중이던 작업은 다음 delay 지점에서 멈춤)"); return true;
                case "sel": Select(cmd.Rest, cmd.Rest.Length == 0 ? null : By.CssSelector(cmd.Rest)); return true;
                case "xpath": Select(cmd.Rest, cmd.Rest.Length == 0 ? null : By.XPath(cmd.Rest)); return true;
                case "url": Url(cmd); return true;
                case "snap": Log($"스냅샷: {SelectorTrace.SaveSnapshot(util.getDriver()) ?? "(브라우저 없음)"}"); return true;
                case "quit":
                case "exit":
                    fl.closeChrome();
                    return false;
                default:
                    Log($"알 수 없는 명령: {cmd.Name} (help 참고)", ConsoleColor.Red);
                    return true;
            }
        }
        catch (Exception ex)
        {
            Log(ex.ToString(), ConsoleColor.Red);
            return true;
        }
    }

    private static void PrintLog(string msg) => Log(msg);

    private static void PrintLogLeft(string msg) => Log($"[상태] {msg}", ConsoleColor.DarkGray);

    private static DevConfig LoadConfig()
    {
        string path = Path.Combine(AppPaths.DataDir, "dev.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"{path} 가 없어요. devdata.example/dev.json 을 복사하세요.");
        }
        return DevConfig.Load(path);
    }

    // bandAccount.txt 형식: 아이디\t Base64(비밀번호)\t 유형 (LoginSecond.buttonAdd_Click과 같음).
    // 비밀번호를 손으로 Base64로 바꾸기 번거로워서 추가 명령을 둔다.
    private void Account(ShellCommand cmd)
    {
        if (cmd.Args.Count >= 4 && cmd.Args[0] == "add")
        {
            string type = cmd.Args[3];
            if (type != "전화번호" && type != "이메일")
            {
                Log("유형은 전화번호 또는 이메일", ConsoleColor.Red);
                return;
            }
            util.writeStream("bandAccount.txt", string.Join("\t", cmd.Args[1], util.Base64Encoding(cmd.Args[2]), type));
            Log($"추가됨: {cmd.Args[1]} ({type})");
            return;
        }
        List<string> rows = util.readAll("bandAccount.txt") ?? new List<string>();
        foreach (string row in rows)
        {
            string[] parts = row.Split('\t');
            Log($"{parts[0]} ({(parts.Length > 2 ? parts[2] : "?")})");
        }
        if (rows.Count == 0) Log("계정 없음. account add <아이디> <비밀번호> <전화번호|이메일>");
    }

    private void Login(ShellCommand cmd)
    {
        if (cmd.Args.Count < 1) { Log("login <아이디>", ConsoleColor.Red); return; }
        string row = (util.readAll("bandAccount.txt") ?? new List<string>())
            .FirstOrDefault(r => r.Split('\t')[0] == cmd.Args[0]);
        if (row == null) { Log($"bandAccount.txt에 {cmd.Args[0]} 가 없어요 (account 로 확인)", ConsoleColor.Red); return; }
        string[] parts = row.Split('\t');
        util.setBandAccount(parts[0], util.Base64Decoding(parts[1]), parts[2]);
        Log(fl.login() ? "로그인 성공 (브라우저는 quit 전까지 유지)" : "로그인 실패");
    }

    private void Bands()
    {
        List<BandInfo> bands = fl.getBandList(new List<BandInfo>());
        if (bands == null) { Log("목록을 가져오지 못했어요", ConsoleColor.Red); return; }
        foreach (BandInfo band in bands) Log($"{band.num}\t{band.name}");
        Log($"총 {bands.Count}개");
    }

    private void Search(ShellCommand cmd)
    {
        if (cmd.Args.Count < 1) { Log("search <검색어> [--cnt N] [--min N --max N]", ConsoleColor.Red); return; }
        int min = cmd.IntOption("min", -1);
        int max = cmd.IntOption("max", -1);
        fl.setBandListFromQuery(cmd.Args[0], cmd.IntOption("cnt", 10), min, max);
        List<BandInfo> found = min < 0 ? fl.getBandListFromQuery() : fl.getBandListFromQueryWithMemCnt();
        if (found == null) { Log("검색 실패", ConsoleColor.Red); return; }
        foreach (BandInfo band in found) Log($"{band.num}\t{band.name}");
        Log($"총 {found.Count}개");
    }

    private void Signup(ShellCommand cmd)
    {
        if (cmd.Args.Count < 2) { Log("signup <밴드URL> <닉네임>", ConsoleColor.Red); return; }
        BandInfo band = fl.getBandInfoFromUrl(cmd.Args[0]);
        Log(band == null ? "밴드 정보를 읽지 못했어요" : fl.signupBand(band, cmd.Args[1]));
    }

    private void StartJob(string label, Action<JobParams> setParams, JobParams config, Action job)
    {
        setParams(config);
        work.Start(() =>
        {
            try { job(); }
            catch (Exception ex) { Log(ex.ToString(), ConsoleColor.Red); }
            Log($"{label} 작업 종료");
        });
        Log($"{label} 작업 시작 (pause / resume / init)");
    }

    private void Select(string text, By by)
    {
        IWebDriver driver = util.getDriver();
        if (driver == null) { Log("브라우저가 없어요 (chrome 또는 login)", ConsoleColor.Red); return; }
        if (by == null || text.Length == 0) { Log("sel <css> / xpath <xpath>", ConsoleColor.Red); return; }
        IReadOnlyCollection<IWebElement> elements = driver.FindElements(by); // SelectorTrace를 거치지 않음
        Log($"{elements.Count}개");
        int i = 0;
        foreach (IWebElement element in elements.Take(5))
        {
            string summary = (element.Text ?? "").Replace('\n', ' ').Trim();
            if (summary.Length > 60) summary = summary[..60] + "…";
            Log($"  [{i++}] <{element.TagName} class=\"{element.GetAttribute("class")}\"> {summary}");
        }
    }

    private void Url(ShellCommand cmd)
    {
        if (util.getDriver() == null) { Log("브라우저가 없어요 (chrome 또는 login)", ConsoleColor.Red); return; }
        if (cmd.Args.Count > 0) util.goToUrl(cmd.Args[0], 0);
        Log(util.currentUrl());
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
          account                             계정 목록 (bandAccount.txt)
          account add <아이디> <비번> <전화번호|이메일>
          login <아이디>                      Chrome 열고 로그인 (quit 전까지 유지)
          chrome                              로그인 없이 Chrome만 열기
          bands                               내 밴드 목록
          search <검색어> [--cnt N] [--min N --max N]
          signup <밴드URL> <닉네임>
          post | comment | chat               dev.json 값으로 작업 시작 (백그라운드)
          pause | resume | init               일시정지 / 재개 / 초기화
          sel <css>  |  xpath <xpath>         현재 페이지에서 셀렉터 시험
          url [주소]                          현재 URL / 이동
          snap                                현재 페이지 스냅샷 저장
          quit                                Chrome 닫고 종료
        """);
    }
}
