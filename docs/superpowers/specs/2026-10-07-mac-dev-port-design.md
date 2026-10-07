# BandProgram 맥 개발환경 포팅 설계

- 작성일: 2026-10-07
- 상태: 검토 대기

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
| `Clipboard.SetText` + STA 스레드 | `Util.sendKeyPaste` | `IClipboard.SetText(string)`. Windows 구현은 UI 프로젝트의 `WinFormsClipboard`(기존 STA 스레드 방식 유지), 맥 구현은 Core의 `PbcopyClipboard`(`pbcopy` 프로세스). `Util.Clipboard` 정적 속성으로 주입하고, 기본값은 OS에 따라 고른다. |
| `Keys.LeftControl + "v"` | `Util.sendKeyPaste` | `PlatformKeys.PasteModifier`: 맥은 `Keys.Command`, 그 외 `Keys.LeftControl`. |
| `WinHttpRequest` COM | `Util.requestHTTP` 2곳 | 공유 `HttpClient`로 동기 GET(`GetStringAsync(...).GetAwaiter().GetResult()`). 쿼리 문자열 조립은 그대로 둔다. |
| `WebRequest` | `APIDAO` 2곳 | 같은 `HttpClient`. 실패 시 `null` 반환 동작은 유지. `Login`/`LoginSecond`/`MainForm`의 `HttpWebRequest`는 .NET 10에서도 동작하므로 그대로 둔다. |
| `"adb.exe"` | `ADB.executeADB` | Windows는 `adb.exe`, 그 외 `adb`. 실행 실패(`Win32Exception`) 시 예외 대신 빈 문자열을 반환하고 `"[ADB] adb를 찾을 수 없어 IP 변경을 건너뜀"` 로그를 남긴다. 호출부 `Util.changeIP`의 `getDeivces()[0]`가 빈 목록에서 터지지 않도록 `getDeivces`는 최소 1개(빈 문자열) 요소를 돌려주는 기존 동작(`Split` 결과)을 유지한다. |
| `Application.StartupPath`, `\\` 결합 | `FunctionList.startPath`, `Util.startChrome` | `AppPaths.DataDir`(기본 `AppContext.BaseDirectory`, Dev에서 `--data`로 변경 가능). `user-data-dir`은 `Path.Combine(DataDir, "chromedata")`, 디스크 캐시는 `Path.Combine(DataDir, "Cache")`. `BandLayout.entirePath`도 `AppPaths.DataDir`을 쓴다. |
| 상대 경로 파일 접근(`"acc.txt"`, `"사용기록.txt"` 등) | 여러 곳 | 바꾸지 않는다. 작업 디렉터리 기준이므로 Dev 셸이 시작할 때 `Directory.SetCurrentDirectory(AppPaths.DataDir)`로 맞춘다. |
| `ChromeDriverService` + 동봉 드라이버 | `Util.startChrome` | Selenium 4 Selenium Manager가 설치된 Chrome에 맞는 드라이버를 받는다. 옵션(창 크기, `disable-gpu` 등)과 `PageLoad` 타임아웃은 그대로. `HideCommandPromptWindow`는 유지. |
| `Process.GetProcessesByName("chromedriver")` Kill | `Util.closeChrome` | 그대로 유지(맥에서도 프로세스 이름이 같다). |
| `MessageBox.Show(str1)` | `FunctionList.getBandInfoFromUrl` | 디버그 잔재로 보고 삭제한다. 이것이 유일한 동작 변경이다(고객에게 URL 팝업이 뜨지 않게 됨). |
| `OpenFileDialog` | `FunctionList.showFileOpenDialog` | UI 프로젝트의 `ImageFileDialog.Show()`로 옮기고 `NewPostForm`, `PostingAddForm` 호출부를 바꾼다. |
| `MenuItem`/`ContextMenu` (.NET Core 3.1에서 삭제됨) | `LoginSecond`, `MainForm`, `BandLayout`, `NewPostForm`, `PostingAddForm` | `ContextMenuStrip`/`ToolStripMenuItem`으로 바꾼다. 클릭 핸들러의 `((MenuItem)obj).Index`는 `Owner.Items.IndexOf(item)`로 바꿔 인덱스 의미를 유지한다. |
| `Process.Start(경로)` | `NewPostForm`, `PostingAddForm` | .NET 10은 `UseShellExecute` 기본값이 `false`라 폴더·이미지 열기가 실패한다. `new ProcessStartInfo(path) { UseShellExecute = true }`로 바꾼다. |

## 5. 일시정지·재개·초기화 (`WorkControl`)

### 문제
`Thread.Suspend/Resume/Abort`는 .NET Core 이후 `PlatformNotSupportedException`을 던진다.
호출부가 `catch {}`로 감싸져 있어 고객에게는 버튼이 눌려도 작업이 계속 도는 것으로 보인다.

### 설계
작업 스레드가 동작 사이마다 반드시 거치는 `Util.delay()`(`FunctionList`에서 55회 호출, `goToUrl` 등
내부에서도 사용)에 확인 지점을 둔다. 흐름 코드는 건드리지 않는다.

```csharp
// BandProgram.Core/Work/WorkControl.cs
public static class WorkControl
{
    // 작업 스레드를 등록하고 실행. 등록된 스레드에서만 delay()가 확인 지점이 된다.
    public static Thread Start(Action work);     // 새 세대 번호 부여, IsBackground = true
    public static void Pause();                  // 게이트 닫기
    public static void Resume();                 // 게이트 열기
    public static void Reset();                  // 세대 번호 증가 + 게이트 열기
    internal static void Checkpoint();           // Util.delay()가 Thread.Sleep 전후에 호출
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
- `BandLayout`: `new Thread(startWork).Start()` → `WorkControl.Start(startWork)`,
  `Suspend` → `Pause`, `Resume` → `Resume`, `Abort` → `Reset`.
  `ThreadState.Suspended`/`Aborted` 검사는 `BandLayout` 내부 상태 필드(`isPaused`, `isReset`)로 바꾼다.
- `MainForm` 가입 작업(`button15`, `button14`, `button10`): 같은 방식.
- 초기화된 작업은 `startWork` 끝의 `refresh()`/`toggleState()`를 실행하지 않는다(기존 `Abort`와 같음).
  버튼 상태는 기존처럼 초기화 핸들러가 직접 `toggleState(true, true)`로 맞춘다.

## 6. 셀렉터 실패 추적 (`SelectorTrace`)

`Util.findElement`/`findElements`/`findElementsWithXPath`와 자식 요소 오버로드에
`[CallerMemberName]`, `[CallerFilePath]`, `[CallerLineNumber]` 선택적 매개변수를 추가한다.
기존 호출부는 수정 없이 컴파일되고, 호출 위치가 자동으로 채워진다.

요소를 못 찾았을 때(단일: 예외, 복수: 0개) 반환값은 그대로 두고 아래를 기록한다.

```
[SELECTOR MISS] "[class='result _bandPageCount']"
  ← FunctionList.getBandListFromQuery (FunctionList.cs:416)
  url: https://band.us/discover/search/캠핑
  snapshot: devdata/failures/20261007-153012-416/{page.html, screen.png}
```

- `SelectorTrace.Sink`(`Action<string>`)로 출력 대상을 정한다. Dev는 콘솔, WinForms는 `사용기록.txt`에 한 줄.
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
band> post | comment | chat   dev.json의 파라미터로 setXxxParam 후 WorkControl.Start(startXxx)
band> pause | resume | init   WorkControl.Pause/Resume/Reset
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
- `.vscode/launch.json`: Dev 셸 디버그 구성과 "All Exceptions" 예외 중단 설정을 커밋한다.
  예외 중단을 켜면 `catch {}`에 삼켜지기 전의 `NoSuchElementException`에서 멈춘다.

## 8. 저장소 정리

- `.gitignore`(Visual Studio + macOS)를 추가하고 `git rm --cached`로 `.vs/`, `bin/`, `obj/`, `packages/`,
  `.DS_Store`를 추적에서 뺀다(로컬 파일은 남는다).
- **보안 주의:** 현재 GitHub 원격 저장소에 `bin/Debug/acc.txt`(프로그램 아이디·비밀번호),
  `bin/Debug/bandAccount.txt`(밴드 계정, Base64), `bin/Debug/chromedata/`(로그인 쿠키)가 올라가 있다.
  추적 해제만으로는 기록에서 지워지지 않는다. 비밀번호 변경을 권장하고, 기록 삭제(`git filter-repo`)는
  강제 푸시가 필요하므로 이 작업과 별개로 사용자가 결정한다.
- `bin/Debug/AutoDoc/` 샘플 중 텍스트 파일은 `BandProgram.Tests/Fixtures/AutoDoc/`으로 옮겨 테스트에 쓴다.

## 9. 검증

1. **빌드:** 맥에서 `dotnet build BandProgram.sln`이 성공한다.
2. **단위 테스트(`BandProgram.Tests`):**
   - `Util.calculateTime`, `FunctionList.stringToIntList`/`intListToString`(private → `internal` +
     `InternalsVisibleTo`), `getPostingList`/`getPostingNum`을 Fixtures 폴더로 실행. 기대값은 원본 코드
     동작에서 가져온다.
   - `WorkControl`: 일시정지 시 등록 스레드가 멈추고 재개 시 진행, 초기화 시 이전 세대가 멈추고 새 세대는 진행,
     미등록 스레드는 영향 없음.
   - `SelectorTrace`: 호출 위치 기록, 1초 중복 억제.
   - Selenium이 필요한 흐름은 단위 테스트하지 않는다.
3. **맥 수동 확인(사용자):** Dev 셸에서 `login` → `bands` → `search` → 테스트 밴드에 `post` →
   `pause`/`resume`/`init` → 일부러 틀린 셀렉터로 `[SELECTOR MISS]` 확인.
4. **Windows 스모크 테스트(사용자):** `dotnet publish BandProgram -c Release -r win-x64 --self-contained`
   산출물을 Windows에서 실행. 체크리스트(라이선스 로그인, 계정 로그인, 밴드 목록, 포스팅 1건,
   일시정지/재개/초기화, 가입 일시정지/초기화, 우클릭 메뉴, 이미지 추가, 폴더 열기)를 구현 계획에 포함한다.

## 10. 작업 순서 (커밋 단위)

1. `.gitignore` 추가, 빌드 산출물·`.vs`·`packages` 추적 해제
2. SDK 스타일 csproj, .NET 10, `PackageReference`(Selenium은 아직 3.141), `EnableWindowsTargeting`
   → `MenuItem`→`ContextMenuStrip`, `Process.Start` 수정으로 빌드 통과
3. `WorkControl` 도입, `Suspend/Resume/Abort` 교체 (+ 테스트)
4. `BandProgram.Core` 분리, 4장 대체 적용 (+ 테스트 프로젝트, 순수 로직 테스트)
5. Selenium 4 업그레이드, 동봉 `chromedriver.exe` 제거
6. `SelectorTrace` (+ 테스트)
7. `BandProgram.Dev` 셸, `devdata.example`, `.vscode/launch.json`
8. Windows 스모크 테스트 체크리스트 문서

## 11. 위험

- **Selenium 4 동작 차이:** 암묵적 대기, 알림창 처리 등 세부 동작이 다를 수 있다. 5단계를 별도 커밋으로
  두어 문제가 생기면 그 단계만 되돌릴 수 있게 한다.
- **WinForms .NET 10 렌더링 차이:** 고해상도(DPI) 처리 기본값이 달라 레이아웃이 어긋날 수 있다.
  `ApplicationHighDpiMode`를 `DpiUnaware`로 두어 기존과 맞추고 스모크 테스트로 확인한다.
- **초기화된 스레드 누적:** 초기화할 때마다 대기 스레드가 하나씩 남는다. 메모리 사용량은 작고,
  프로그램을 재시작하면 정리된다.
- **맥에서 Windows 실행 확인 불가:** Windows 동작은 사용자의 스모크 테스트에 의존한다.
