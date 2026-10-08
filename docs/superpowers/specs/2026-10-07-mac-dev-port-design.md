# BandProgram 맥 개발환경 포팅 설계

- 작성일: 2026-10-07
- 상태: 승인됨 (2026-10-07), 시험 빌드 결과 반영

## 1. 목적과 범위

### 목적
맥에서 BandProgram의 자동화 로직(로그인, 밴드 검색·가입, 포스팅, 댓글, 채팅)을 실행하고
브레이크포인트로 디버깅할 수 있게 한다. 특히 밴드 화면 변경으로 **어느 셀렉터가 깨졌는지
모르는 상황**을 빠르게 찾고 고칠 수 있어야 한다.

### 제약
- 고객은 Windows를 쓴다. 배포물은 계속 Windows용 WinForms exe다.
- 개발자는 맥에서 개발·디버깅한다. 맥에서 WinForms UI를 띄울 필요는 없다.
- 자동화 로직(흐름, 셀렉터, 대기 시간)은 바꾸지 않는다. 이 문서에 명시한 대체·추가만 한다.

### 성공 기준
1. 맥에서 `dotnet build`로 솔루션 전체(WinForms 포함)가 빌드된다.
2. 맥에서 Dev 셸로 로그인 → 밴드 목록 → 검색 → 포스팅/댓글/채팅을 실행하고, 일시정지·재개·초기화하고,
   브레이크포인트를 걸 수 있다.
3. 셀렉터가 깨지면 콘솔에 셀렉터·호출 위치(파일:줄)·URL이 출력되고 페이지 스냅샷이 저장된다.
4. 맥에서 `dotnet publish -r win-x64 --self-contained`로 만든 exe가 Windows에서 기존과 같이 동작한다
   (시작/일시정지/초기화 포함).

### 범위 밖
- 맥용 UI(Avalonia 등), 언어 변경
- 셀렉터 수정, 기능 추가, 리팩터링(이 문서에 명시한 것 제외)
- PHP 라이선스 서버(`band/`)

## 2. 현재 상태 요약

- .NET Framework 4.8, WinForms, 약 9,700줄. 핵심 로직은 `FunctionList.cs`(2,400줄)와 `Util.cs`(1,200줄).
- Selenium 3.141 + 저장소에 동봉된 `chromedriver.exe`(91 버전). `Util`은 드라이버 하나를 가진 싱글톤.
- 브라우저 생명주기: `LoginSecond`에서 계정 로그인 시 Chrome이 열리고(`fl.login` → `closeChrome` → `startChrome`),
  이후 모든 작업이 같은 드라이버를 공유한다. 작업이 실패해도 브라우저는 닫지 않는다.
  `closeChrome`은 재로그인, 계정 변경(`MainForm.button16`), 창 닫기, 라이선스 세션 끊김에서만 호출된다.
  **이 동작은 그대로 유지한다.**
- 작업 제어: 포스팅·댓글·채팅은 `BandLayout`, 가입은 `MainForm`이 전용 스레드를 만들고
  `Thread.Suspend/Resume/Abort`로 일시정지·재개·초기화한다. 한 번에 하나의 작업만 돈다
  (`MainForm.isOperating`이 다른 작업 버튼을 비활성화).
- 오류 처리: `Util.findElement`는 못 찾으면 `null`을 돌려주고, `catch {}`가 `FunctionList`에 54개,
  `Util`에 39개 있어서 셀렉터가 깨져도 어디가 깨졌는지 드러나지 않는다.

## 3. 프로젝트 구조

```
BandProgram.sln
├─ BandProgram.Core/      net10.0          맥·윈도우 공용, WinForms 의존 없음
│    FunctionList, Util, APIDAO, ADB, Naver, NaverMobile, Global, AppConfiguration
│    모델: AccountInfo, Band, BandInfo, ImageFile, IntCompare, Post, Response
│    Platform/   IClipboard, PlatformKeys, AppPaths
│    Work/       WorkControl
│    Diagnostics/SelectorTrace
├─ BandProgram/           net10.0-windows  WinForms, 고객 배포용
│    Program, Login, LoginSecond, MainForm, BandLayout,
│    NewPostForm, PostingAddForm, SelectPostingForm, WinFormsClipboard
├─ BandProgram.Dev/       net10.0          맥 디버깅용 대화형 셸
└─ BandProgram.Tests/     net10.0          xUnit
```

- 대상 프레임워크는 .NET 10(LTS, 2028-11 지원 종료). .NET 8은 2026-11 지원 종료라 쓰지 않는다.
- 모든 csproj를 SDK 스타일로 바꾸고 `PackageReference`를 쓴다. `packages/`, `packages.config`,
  `chromedriver.exe`, `WinHttp` COM 참조, `Properties/AssemblyInfo.cs`(SDK가 생성)는 제거한다.
- `BandProgram.csproj`에 `EnableWindowsTargeting=true`를 켜서 맥에서도 빌드되게 한다.
- `BandLayout`은 WinForms 컨트롤(버튼, 리스트뷰)을 다루므로 UI 프로젝트에 남긴다.
- 패키지: `Selenium.WebDriver` 4.x, `Selenium.Support` 4.x(`SelectElement` 사용), `Newtonsoft.Json` 13.x,
  `System.Configuration.ConfigurationManager`.

## 4. Windows 전용 코드 대체

| 현재 | 위치 | 변경 |
|---|---|---|
| `Clipboard.SetText` + STA 스레드 | `Util.sendKeyPaste` | `IClipboard.SetText(string)`. Windows 구현은 UI 프로젝트의 `WinFormsClipboard`(기존 STA 스레드 방식 유지), 맥 구현은 Core의 `PbcopyClipboard`(`pbcopy` 프로세스, `LANG=en_US.UTF-8` 필수: 없으면 한글이 들어가지 않음). `Util.Clipboard` 정적 속성으로 주입하고, 기본값은 맥이면 `PbcopyClipboard`, 그 외 `null`(WinForms `Program`이 설정). |
| `element.SendKeys(Keys.LeftControl + "v")` | `Util.sendKeyPaste` | Windows는 그대로. 맥은 `element.SendKeys(Keys.Command + "v")`가 동작하지 않아(시험 확인), JS로 요소에 `focus()` 후 `Actions.KeyDown(Keys.Command).SendKeys("v").KeyUp(Keys.Command)`를 쓴다. 헤드리스 Chrome에서는 붙여넣기가 되지 않는다. |
| `Encoding.Default` | `Util` 6곳(`readAll`, `readALine`, `readAllToString`, `writeStream`, `createNotePad`, `firstLineToBack`) | `AppText.Encoding`(UTF-8, BOM 없음)으로 명시한다. 기존 CP949 파일 처리는 4-1장. |
| `WinHttpRequest` COM | `Util.requestHTTP` 2곳 | 공유 `HttpClient`로 동기 GET. WinHttp처럼 HTTP 오류 상태에서도 본문을 돌려준다(`GetAsync` + `ReadAsStringAsync`, 상태 코드 검사 없음). 쿼리 문자열 조립은 그대로 둔다. COM 참조는 `dotnet build`에서 빌드되지 않으므로 csproj 전환과 같은 단계에서 바꾼다. |
| `WebRequest` | `APIDAO`, `Login`, `LoginSecond`, `MainForm` | .NET 10에서도 맥·윈도우 모두 동작하므로 그대로 둔다(SYSLIB0014 경고만 남음). |
| `"adb.exe"` | `ADB.executeADB` | Windows는 `adb.exe`, 그 외 `adb`. 실행 실패 시 예외는 기존처럼 호출부로 전파한다. 예외를 삼키면 `Util.changeIP(preIP, appId)`가 IP가 바뀔 때까지 자기 자신을 재귀 호출해 스택 오버플로가 나기 때문이다. 참고로 `changeIP`는 현재 어디에서도 호출되지 않는다. |
| `Application.StartupPath`, `\\` 결합 | `FunctionList.startPath`, `Util.startChrome` | `AppPaths.DataDir`(기본 `AppContext.BaseDirectory`, Dev에서 `--data`로 변경 가능). `user-data-dir`은 `Path.Combine(DataDir, "chromedata")`, 디스크 캐시는 `Path.Combine(DataDir, "Cache")`. `BandLayout.entirePath`도 `AppPaths.DataDir`을 쓴다. |
| 상대 경로 파일 접근(`"acc.txt"`, `"사용기록.txt"` 등) | 여러 곳 | 바꾸지 않는다. 작업 디렉터리 기준이므로 Dev 셸이 시작할 때 `Directory.SetCurrentDirectory(AppPaths.DataDir)`로 맞춘다. |
| `ChromeDriverService` + 동봉 드라이버 | `Util.startChrome` | Selenium 4 Selenium Manager가 설치된 Chrome에 맞는 드라이버를 받는다. 옵션(창 크기, `disable-gpu` 등)과 `PageLoad` 타임아웃은 그대로. `HideCommandPromptWindow`는 유지. |
| `Process.GetProcessesByName("chromedriver")` Kill | `Util.closeChrome` | 그대로 유지(맥에서도 프로세스 이름이 같다). |
| `MessageBox.Show(str1)` | `FunctionList.getBandInfoFromUrl` | 디버그 잔재로 보고 삭제한다. 이것이 유일한 동작 변경이다(고객에게 URL 팝업이 뜨지 않게 됨). |
| `OpenFileDialog` | `FunctionList.showFileOpenDialog` | UI 프로젝트의 `ImageFileDialog.Show()`로 옮기고 `NewPostForm`, `PostingAddForm` 호출부를 바꾼다. |
| `MenuItem`/`ContextMenu` (.NET 10에서 컴파일은 되지만 실행 시 `PlatformNotSupportedException`, 경고 WFDEV006) | `LoginSecond`, `MainForm`, `BandLayout`, `NewPostForm`, `PostingAddForm` | `ContextMenuStrip`/`ToolStripMenuItem`으로 바꾼다. 클릭 핸들러의 `((MenuItem)obj).Index`는 `Owner.Items.IndexOf(item)`로 바꿔 인덱스 의미를 유지한다. |
| `Process.Start(경로)` | `NewPostForm`, `PostingAddForm` | .NET 10은 `UseShellExecute` 기본값이 `false`라 폴더·이미지 열기가 실패한다. `new ProcessStartInfo(path) { UseShellExecute = true }`로 바꾼다. |
| WinForms 기본 글꼴·DPI | `Program.Main` | .NET Core 3.0부터 기본 글꼴이 Segoe UI 9pt로 바뀌어 레이아웃이 어긋난다. .NET Framework처럼 시스템 기본 글꼴(`SystemFonts.DefaultFont`, 한국어 Windows는 굴림 9pt — 폼의 `AutoScaleDimensions 7x12`와 같음)을 `Application.SetDefaultFont`로 지정하고, `Application.SetHighDpiMode(HighDpiMode.DpiUnaware)`로 DPI 동작을 맞춘다. |
| `Properties/AssemblyInfo.cs`, `App.config`의 `<startup>` | | SDK가 어셈블리 정보를 생성하므로 삭제하고 제목·버전은 csproj로 옮긴다. `<startup>`은 .NET 10에서 의미가 없어 제거한다. |

## 4-1. 텍스트 파일 UTF-8 전환

모든 텍스트 파일은 UTF-8(BOM 없음)로만 읽고 쓴다. 맥에서 VS Code로 원고와 목록 파일을 바로 편집하기 위해서다.

### 문제
.NET Framework 버전은 `Encoding.Default`(한국어 Windows에서 CP949)로 파일을 저장했다. 고객 PC의
`bandList.txt`, `bandAccount.txt`, `AutoDoc/**/contents.txt`가 모두 CP949임을 확인했다.
UTF-8로만 읽으면 업데이트 직후 고객의 한글 데이터가 모두 깨진다.

### 설계: 시작 시 1회 변환 (`Utf8Migration`)
- 대상: 데이터 폴더 최상위의 `*.txt`와 `AutoDoc/` 아래 모든 `*.txt`. `chromedata/`, `backup-cp949/` 등 다른 폴더는 보지 않는다.
- 판별: 엄격한 UTF-8 디코딩에 성공하면(ASCII, BOM 있는 UTF-8 포함) 그대로 둔다. 실패하면 CP949로 보고 변환한다.
- 변환: 원본을 `backup-cp949/<같은 상대 경로>`에 복사한 뒤(이미 있으면 덮어쓰지 않음, 처음 원본 보존),
  임시 파일에 UTF-8로 쓰고 원래 파일과 바꾼다.
- 실행 시점: WinForms `Program.Main`과 Dev 셸 시작 시, 어떤 파일도 읽기 전에 실행한다. 두 번째 실행부터는 바꿀 파일이 없다.
- 기록: WinForms는 변환·실패 목록을 `encoding-migration.log`에 남기고, 실패가 있으면 메시지 상자로 알린다. Dev 셸은 콘솔에 출력한다.
- 파일 하나가 실패해도 나머지는 계속 변환하고 프로그램은 시작한다.

### 남는 위험
- 변환 뒤 구버전 exe로 되돌리면 한글이 깨진다. `backup-cp949/`의 원본을 되돌려 넣으면 복구된다.
- 한두 글자뿐인 아주 짧은 CP949 파일은 우연히 올바른 UTF-8로 판별돼 변환되지 않을 수 있다. 실제 원고와 목록 파일에서는 사실상 일어나지 않는다.

## 5. 일시정지·재개·초기화 (`WorkControl`)

### 문제
`Thread.Suspend/Resume/Abort`는 .NET Core 이후 `PlatformNotSupportedException`을 던진다.
포스팅이 일시정지된 동안 댓글 작업을 시작할 수 있으므로(기존 동작), 작업 제어는 전역이 아니라
작업 종류마다 하나씩 갖는 인스턴스로 만든다.
호출부가 `catch {}`로 감싸져 있어 고객에게는 버튼이 눌려도 작업이 계속 도는 것으로 보인다.

### 설계
작업 스레드가 동작 사이마다 반드시 거치는 `Util.delay()`(`FunctionList`에서 55회 호출, `goToUrl` 등
내부에서도 사용)에 확인 지점을 둔다. 흐름 코드는 건드리지 않는다.

```csharp
// BandProgram.Core/Work/WorkControl.cs
public sealed class WorkControl
{
    // 작업 스레드를 등록하고 실행. 등록된 스레드에서만 delay()가 확인 지점이 된다.
    public Thread Start(Action work);            // 새 세대 번호 부여, IsBackground = true
    public void Pause();                         // 게이트 닫기
    public void Resume();                        // 게이트 열기
    public void Reset();                         // 세대 번호 증가 + 게이트 열기
    public static void Checkpoint();             // Util.delay()가 호출. 현재 스레드의 WorkControl을 확인
}
```

- **등록된 스레드만** 확인 지점에서 멈춘다. UI 스레드, 밴드 목록 로딩 스레드, 라이선스 확인 스레드
  (`MainForm.loginCheck`, 자체 `Thread.Sleep` 사용)는 영향이 없다. 등록은 `[ThreadStatic]` 세대 번호로 한다.
- **일시정지:** `Checkpoint()`가 `ManualResetEventSlim` 게이트가 열릴 때까지 기다린다.
- **초기화:** 세대 번호가 바뀐 스레드는 `Checkpoint()`에서 영구히 대기한다(백그라운드 스레드라
  프로그램 종료를 막지 않는다). 예외를 던지지 않는 이유는 54개의 `catch {}`가 예외를 삼키고
  다음 단계(예: 글 등록 클릭)를 실행할 위험이 있기 때문이다.
- **긴 대기 처리:** `Thread.Sleep(MS)`를 최대 200ms 단위로 나눠 자면서 매번 `Checkpoint()`를 호출한다.
  예약 대기처럼 긴 `delay`도 바로 멈춘다.
- **동작 차이:** 기존은 버튼을 누르는 즉시 멈췄지만, 이제는 진행 중이던 Selenium 호출 하나가 끝난 뒤
  다음 확인 지점에서 멈춘다(보통 수백 ms).

### 호출부 변경
- `BandLayout`: 인스턴스마다 `WorkControl work` 필드를 두고 `new Thread(startWork).Start()` → `work.Start(startWork)`,
  `Suspend` → `Pause`, `Resume` → `Resume`, `Abort` → `Reset`.
  `ThreadState.Suspended`/`Aborted` 검사는 `BandLayout` 내부 상태 필드(`isPaused`, `isReset`)로 바꾼다.
- `MainForm` 가입 작업(`button15`, `button14`, `button10`): `signupWork` 필드로 같은 방식.
- 초기화된 작업은 `startWork` 끝의 `refresh()`/`toggleState()`를 실행하지 않는다(기존 `Abort`와 같음).
  버튼 상태는 기존처럼 초기화 핸들러가 직접 `toggleState(true, true)`로 맞춘다.

## 6. 셀렉터 실패 추적 (`SelectorTrace`)

`Util.findElement`/`findElements`/`findElementsWithXPath`와 자식 요소 오버로드에서 실패를 감지하고,
호출 위치는 `StackTrace`에서 `Util`이 아닌 첫 프레임(파일·줄 포함)으로 찾는다. `[CallerLineNumber]` 방식은
`clickByCss`, `delayNext`, `sendKey(css, ...)` 같은 `Util` 내부 래퍼를 거치면 래퍼 위치만 남아서 쓰지 않는다.
메서드 시그니처는 바뀌지 않는다.

요소를 못 찾았을 때(단일: 예외, 복수: 0개) 반환값은 그대로 두고 아래를 기록한다.

```
[SELECTOR MISS] "[class='result _bandPageCount']"
  ← FunctionList.getBandListFromQuery (FunctionList.cs:416)
  url: https://band.us/discover/search/캠핑
  snapshot: devdata/failures/20261007-153012-416/{page.html, screen.png}
```

- `SelectorTrace.Sink`(`Action<string>`)로 출력 대상을 정한다. Dev는 콘솔, WinForms는 `selector-miss.log`에 추가한다
  (고객이 보는 `사용기록.txt`를 어지럽히지 않기 위해).
- `SelectorTrace.SnapshotEnabled`: Dev에서만 `true`. HTML(`driver.PageSource`)과 스크린샷을 저장한다.
- 같은 위치에서 1초 안에 반복된 실패는 한 번만 기록한다(대기 루프에서 `findElements`를 반복 호출하는 경우).
- `findElements`가 0개를 돌려주는 것이 정상인 경우(존재 여부 확인)도 기록된다. 로그일 뿐 동작은 같다.
- 추적 코드 자체의 예외는 모두 삼켜서 원래 흐름에 영향을 주지 않는다.

## 7. Dev 셸 (`BandProgram.Dev`)

한 번 띄워 두고 명령을 계속 입력하는 대화형 셸이다. 실제 앱처럼 로그인한 브라우저를 유지한 채로 여러 작업을 한다.

```
dotnet run --project BandProgram.Dev -- [--data ./devdata]

band> login <아이디>          bandAccount.txt에서 계정을 찾아 fl.login() (Chrome 열림, 유지)
band> chrome                  로그인 없이 Chrome만 열기
band> bands                   fl.getBandList() 결과 출력
band> search <검색어> [--min N --max N]
band> signup <밴드URL> <닉네임>
band> post | comment | chat   dev.json의 파라미터로 setXxxParam 후 work.Start(startXxx)
band> pause | resume | init   work.Pause/Resume/Reset (셸 전체에서 WorkControl 하나)
band> sel <css>               현재 페이지에서 셀렉터 실행 → 개수와 앞 5개의 텍스트 요약
band> xpath <xpath>           같은 기능, XPath
band> url [주소]              현재 URL 출력 / 이동
band> snap                    현재 페이지 스냅샷 저장
band> quit                    Chrome 닫고 종료 (이때만 닫힘)
```

- 작업은 백그라운드로 돌아서, 작업 중에도 `pause`, `sel`을 입력할 수 있다.
- 로그 콜백(`printLog`, `printLogLeft`)은 시간을 붙여 콘솔에 출력한다.
- 라이선스 로그인(newsoft.kr)은 거치지 않는다. 라이선스 확인은 UI 계층에만 있다.
- `devdata/`는 `.gitignore`에 넣는다. `devdata.example/`에 `dev.json` 예시와 빈 `AutoDoc` 폴더 구조를 커밋한다.
- 포스팅·댓글·채팅은 실제로 작성된다. 테스트용 밴드에서 실행한다.
- `.vscode/launch.json`, `.vscode/tasks.json`: Dev 셸 디버그 구성을 커밋한다. 예외 중단("All Exceptions")은
  launch.json으로 설정할 수 없어 `docs/dev-on-mac.md`에 VS Code 중단점 패널에서 켜는 방법을 적는다.

## 8. 저장소 정리

- `.gitignore`(Visual Studio + macOS)를 추가하고 `git rm --cached`로 `.vs/`, `bin/`, `obj/`, `packages/`,
  `.DS_Store`를 추적에서 뺀다(로컬 파일은 남는다).
- **보안 주의:** 현재 GitHub 원격 저장소에 `bin/Debug/acc.txt`(프로그램 아이디·비밀번호),
  `bin/Debug/bandAccount.txt`(밴드 계정, Base64), `bin/Debug/chromedata/`(로그인 쿠키)가 올라가 있다.
  추적 해제만으로는 기록에서 지워지지 않는다. 비밀번호 변경을 권장하고, 기록 삭제(`git filter-repo`)는
  강제 푸시가 필요하므로 이 작업과 별개로 사용자가 결정한다.
- 테스트용 포스팅 폴더는 실제 원고를 쓰지 않고, 테스트가 임시 폴더에 파일을 만들어 쓴다.

## 9. 검증

1. **빌드:** 맥에서 `dotnet build BandProgram.sln`이 성공하고, 실행 시 실패하는 API 경고
   (`WFDEV006`, `SYSLIB0006`, `CS0618` Suspend/Resume)가 0개다.
2. **단위 테스트(`BandProgram.Tests`):**
   - `Util.calculateTime`, `FunctionList.stringToIntList`/`intListToString`(private → `internal` +
     `InternalsVisibleTo`), `getPostingList`/`getPostingNum`을 임시 폴더에 만든 원고로 실행. `Utf8Migration`: CP949 변환과 백업,
     UTF-8·ASCII·BOM 파일 유지, 재실행 시 변경 없음, 대상 폴더 범위, 기존 백업 보존. 기대값은 원본 코드
     동작에서 가져온다.
   - `WorkControl`: 일시정지 시 등록 스레드가 멈추고 재개 시 진행, 초기화 시 이전 세대가 멈추고 새 세대는 진행,
     미등록 스레드는 영향 없음, 서로 다른 인스턴스는 독립.
   - `SelectorTrace`: 호출 위치 기록, 1초 중복 억제.
   - Selenium이 필요한 흐름은 단위 테스트하지 않는다.
3. **맥 수동 확인(사용자):** Dev 셸에서 `login` → `bands` → `search` → 테스트 밴드에 `post` →
   `pause`/`resume`/`init` → 일부러 틀린 셀렉터로 `[SELECTOR MISS]` 확인.
4. **Windows 스모크 테스트(사용자):** `dotnet publish BandProgram -c Release -r win-x64 --self-contained`
   산출물을 Windows에서 실행. 체크리스트(라이선스 로그인, 계정 로그인, 밴드 목록, 포스팅 1건,
   일시정지/재개/초기화, 가입 일시정지/초기화, 우클릭 메뉴, 이미지 추가, 폴더 열기)를 구현 계획에 포함한다.

## 10. 작업 순서 (커밋 단위)

1. `.gitignore` 추가, 빌드 산출물·`.vs`·`packages` 추적 해제
2. SDK 스타일 csproj, .NET 10(Selenium은 아직 3.141): WinHttp→HttpClient, UTF-8 명시, 글꼴·DPI,
   `MenuItem`→`ContextMenuStrip`, `Process.Start`
3. `BandProgram.Core` 분리, 나머지 4장 대체 적용, CP949→UTF-8 변환 (+ 테스트 프로젝트). 2와 3 사이 커밋은 배포하지 않는다.
4. `WorkControl` 도입, `Suspend/Resume/Abort` 교체 (+ 테스트). 맥에서 테스트하려면 Core가 먼저 있어야 해서 3과 순서를 바꿨다.
5. `SelectorTrace` (+ 테스트)
6. `BandProgram.Dev` 셸(`account add`로 Base64 비밀번호 계정 추가 포함), `devdata.example`, `.vscode`, `docs/dev-on-mac.md`
7. Selenium 4 업그레이드, 동봉 `chromedriver.exe` 제거. Dev 셸로 맥에서 Chrome 실행을 검증하기 위해 뒤로 옮겼다.
8. Windows 게시 스크립트와 스모크 테스트 체크리스트

## 11. 위험

- **Selenium 4 동작 차이:** 암묵적 대기, 알림창 처리 등 세부 동작이 다를 수 있다. 5단계를 별도 커밋으로
  두어 문제가 생기면 그 단계만 되돌릴 수 있게 한다.
- **WinForms .NET 10 렌더링 차이:** 고해상도(DPI) 처리 기본값이 달라 레이아웃이 어긋날 수 있다.
  `ApplicationHighDpiMode`를 `DpiUnaware`로 두어 기존과 맞추고 스모크 테스트로 확인한다.
- **초기화된 스레드 누적:** 초기화할 때마다 대기 스레드가 하나씩 남는다. 메모리 사용량은 작고,
  프로그램을 재시작하면 정리된다.
- **맥에서 Windows 실행 확인 불가:** Windows 동작은 사용자의 스모크 테스트에 의존한다.
