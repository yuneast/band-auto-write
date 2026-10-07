# BandProgram 맥 개발환경 포팅 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** .NET Framework 4.8 WinForms 앱을 .NET 10으로 옮기고 자동화 로직을 공용 라이브러리로 분리해서, 맥에서 대화형 Dev 셸로 실행·디버깅할 수 있게 한다. 고객용 Windows exe는 기존처럼 동작해야 한다.

**Architecture:** `BandProgram.Core`(net10.0, 자동화 로직)를 WinForms 앱(`net10.0-windows`, 고객용)과 Dev 셸(`net10.0`, 맥 디버깅용)이 함께 참조한다. Windows 전용 API는 작은 플랫폼 클래스로 바꾸고, `Thread.Suspend/Resume/Abort`는 `Util.delay()` 안의 확인 지점(`WorkControl`)으로 대체한다. 셀렉터 실패는 `SelectorTrace`가 호출 위치와 함께 기록한다.

**Tech Stack:** .NET SDK 10 (C#), WinForms, Selenium.WebDriver 3.141 → 4.50.0, xUnit, Newtonsoft.Json

**Spec:** `docs/superpowers/specs/2026-10-07-mac-dev-port-design.md`

## Global Constraints

- 대상 프레임워크: Core·Dev·Tests는 `net10.0`, WinForms는 `net10.0-windows` + `EnableWindowsTargeting=true`.
- 자동화 로직(흐름, 셀렉터, 대기 시간)은 바꾸지 않는다. 이 계획에 적힌 변경만 한다.
- 네임스페이스는 모든 프로젝트에서 기존과 같은 `BandProgram`을 쓴다(Dev는 `BandProgram.Dev`).
- 텍스트 파일 인코딩은 기존 데이터와 같은 CP949(`LegacyText.Encoding`)를 쓴다.
- 브라우저는 재로그인, 계정 변경, 창 닫기, 라이선스 세션 끊김, Dev `quit`에서만 닫힌다.
- 모든 명령은 저장소 루트(`/Users/yundongjun/Documents/GitHub/band-auto-write`)에서 실행한다.
- 빌드 로그는 한국어로 나온다. 성공은 `빌드했습니다.`와 `오류 0개`, 테스트 성공은 `통과!`(또는 `Passed!`)로 확인한다.
- 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`를 붙인다.

## Review Focus

1. **기존 고객 데이터의 한글:** CP949로 저장된 `bandList.txt`, `bandAccount.txt`, `AutoDoc/**/contents.txt`를 읽고 쓰면 한글이 그대로여야 한다. 테스트: Task 2 `LegacyTextTests`, Task 3 `PostingFolderTests`.
2. **일시정지 중 다른 작업 시작:** 포스팅을 일시정지한 상태에서 댓글을 시작해도, 재개하면 포스팅이 이어져야 한다. 테스트: Task 4 `Instances_are_independent`.
3. **긴 예약 대기 중 일시정지·초기화:** `delay(수십 분)` 안에서도 바로 멈춰야 한다. 테스트: Task 4 `Pause_inside_long_delay_stops_promptly`.
4. **UI 스레드와 목록 로딩 스레드:** 일시정지 중에도 `delay()`를 부르는 비작업 스레드는 멈추지 않아야 한다. 테스트: Task 4 `Unregistered_thread_is_not_affected`.
5. **셀렉터 실패 기록이 원래 흐름을 깨지 않음:** 드라이버가 없거나 알림창이 떠 있어서 URL·스크린샷을 못 얻어도 예외가 밖으로 나가지 않아야 한다. 테스트: Task 5 `Miss_without_driver_still_logs_and_never_throws`.

---

## File Structure

```
.gitignore                                  (Task 1) Visual Studio + macOS
BandProgram.sln                             (Task 3, 6) Core/Tests/Dev 추가
BandProgram/                                WinForms 앱 (고객용)
  BandProgram.csproj                        (Task 2) SDK 스타일로 교체
  Program.cs                                (Task 2, 3, 5) 글꼴·DPI, 클립보드, SelectorTrace 설정
  LegacyMenu.cs                (create)     (Task 2) MenuItem 대체 헬퍼
  ShellOpen.cs                 (create)     (Task 2) Process.Start(UseShellExecute) 헬퍼
  WinFormsClipboard.cs         (create)     (Task 3) IClipboard Windows 구현
  ImageFileDialog.cs           (create)     (Task 3) FunctionList.showFileOpenDialog 이전
  BandLayout.cs                             (Task 2, 3, 4) 메뉴, 경로, WorkControl
  MainForm.cs, LoginSecond.cs,
  NewPostForm.cs, PostingAddForm.cs         (Task 2, 3, 4) 메뉴, Process.Start, 파일 대화상자, WorkControl
  Properties/AssemblyInfo.cs   (delete)     (Task 2)
  chromedriver.exe             (delete)     (Task 7)
BandProgram.Core/                           (Task 3) 공용 로직
  BandProgram.Core.csproj
  ADB.cs APIDAO.cs AccountInfo.cs AppConfiguration.cs Band.cs BandInfo.cs FunctionList.cs
  Global.cs ImageFile.cs Naver.cs NaverMobile.cs Post.cs Response.cs Util.cs LegacyText.cs  (git mv)
  Platform/AppPaths.cs Platform/IClipboard.cs Platform/PbcopyClipboard.cs
  Work/WorkControl.cs                       (Task 4)
  Diagnostics/SelectorTrace.cs              (Task 5)
BandProgram.Tests/                          (Task 3~6) xUnit
BandProgram.Dev/                            (Task 6) 대화형 셸
  Program.cs DevShell.cs ShellCommand.cs DevConfig.cs
devdata.example/dev.json                    (Task 6)
.vscode/launch.json .vscode/tasks.json      (Task 6)
docs/dev-on-mac.md                          (Task 6) 맥 디버깅 안내
scripts/publish-windows.sh                  (Task 8)
docs/windows-smoke-test.md                  (Task 8)
```

---

### Task 1: 저장소 정리 (.gitignore, 빌드 산출물 추적 해제)

**Files:**
- Create: `.gitignore`
- Untrack (로컬 파일은 유지): `.vs/`, `BandProgram/.vs/`, `BandProgram/bin/`, `BandProgram/obj/`, `packages/`, `.DS_Store`

**Interfaces:** 없음

- [ ] **Step 1: 추적 중인 산출물 확인**

Run: `git ls-files | grep -cE '^(\.vs/|BandProgram/\.vs/|BandProgram/bin/|BandProgram/obj/|packages/)'`
Expected: 0보다 큰 숫자(약 600)

- [ ] **Step 2: `.gitignore` 작성**

```gitignore
# Build outputs
bin/
obj/
out/
publish/

# Visual Studio / Rider
.vs/
*.suo
*.user
.idea/

# NuGet (legacy packages.config folder)
packages/

# macOS
.DS_Store

# Dev shell data (accounts, cookies, snapshots)
devdata/

# Logs written next to the app
selector-miss.log
```

- [ ] **Step 3: 추적 해제**

```bash
git rm -r -q --cached .vs BandProgram/.vs BandProgram/bin BandProgram/obj packages
git ls-files | grep '\.DS_Store$' | xargs -r git rm -q --cached
```

- [ ] **Step 4: 확인**

Run: `git ls-files | grep -cE '^(\.vs/|BandProgram/\.vs/|BandProgram/bin/|BandProgram/obj/|packages/)|\.DS_Store$'`
Expected: `0`

Run: `ls BandProgram/bin/Debug/bandList.txt`
Expected: 파일이 로컬에 그대로 있음

- [ ] **Step 5: Commit**

```bash
git add .gitignore
git commit -m "Add .gitignore and untrack build outputs, IDE state, and packages

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: SDK 스타일 csproj와 .NET 10 전환 (단일 WinForms 프로젝트)

이 Task가 끝나면 `BandProgram` 하나가 `net10.0-windows`로 맥에서 빌드되고, 런타임이 바뀌면서 생기는 동작 차이(한글 인코딩, 우클릭 메뉴, 파일 열기, 글꼴)가 모두 메워져 있다. Selenium은 아직 3.141이다.

**Files:**
- Replace: `BandProgram/BandProgram.csproj`
- Delete: `BandProgram/packages.config`, `BandProgram/Properties/AssemblyInfo.cs`, `BandProgram/.vs/` 폴더(로컬)
- Modify: `BandProgram/App.config` (`<startup>` 제거)
- Create: `BandProgram/LegacyText.cs`, `BandProgram/LegacyMenu.cs`, `BandProgram/ShellOpen.cs`
- Modify: `BandProgram/Util.cs` (WinHttp → HttpClient, `Encoding.Default` → `LegacyText.Encoding`)
- Modify: `BandProgram/Program.cs` (글꼴·DPI)
- Modify: `BandProgram/LoginSecond.cs`, `MainForm.cs`, `BandLayout.cs`, `NewPostForm.cs`, `PostingAddForm.cs` (메뉴)
- Modify: `BandProgram/NewPostForm.cs`, `PostingAddForm.cs` (`Process.Start`)

**Interfaces:**
- Produces: `LegacyText.Encoding` (`System.Text.Encoding`, CP949), `LegacyMenu.Create(EventHandler onClick, params string[] labels) : ContextMenuStrip`, `LegacyMenu.IndexOf(object sender) : int`, `ShellOpen.Open(string path) : void`

이 Task는 WinForms 프로젝트라 맥에서 테스트를 실행할 수 없다. 검증은 빌드와 "실행 시 실패하는 API" 경고 0개로 한다. `LegacyText` 테스트는 Task 3에서 Core로 옮긴 뒤 추가한다.

- [ ] **Step 1: 빌드가 실패하는 것부터 확인**

Run: `dotnet build BandProgram/BandProgram.csproj 2>&1 | tail -3`
Expected: 실패(구식 csproj라 `Microsoft.CSharp.targets`나 COM 참조 관련 오류)

- [ ] **Step 2: csproj 교체**

`BandProgram/BandProgram.csproj` 전체를 아래로 바꾼다.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <RootNamespace>BandProgram</RootNamespace>
    <AssemblyName>BandProgram</AssemblyName>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <AssemblyTitle>BandProgram</AssemblyTitle>
    <Product>BandProgram</Product>
    <Copyright>Copyright © 2018</Copyright>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Selenium.WebDriver" Version="3.141.0" />
    <PackageReference Include="Selenium.Support" Version="3.141.0" />
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="chromedriver.exe" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

```bash
git rm -q BandProgram/packages.config BandProgram/Properties/AssemblyInfo.cs
rm -rf BandProgram/.vs BandProgram/obj
```

`BandProgram/App.config`를 아래로 바꾼다(`<startup>`은 .NET 10에서 의미 없음).

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
</configuration>
```

- [ ] **Step 3: 빌드해서 남은 오류 확인**

Run: `dotnet build BandProgram/BandProgram.csproj 2>&1 | grep -E " error " | sed -E 's/ \[.*\]//' | sort -u`
Expected: `Util.cs(17,7): error CS0246: 'WinHttp' ...` 한 종류만 나온다.

- [ ] **Step 4: `LegacyText` 추가**

`BandProgram/LegacyText.cs`:

```csharp
using System.Text;

namespace BandProgram
{
	// .NET Framework의 Encoding.Default는 한국어 Windows에서 CP949였다.
	// .NET 10의 Encoding.Default는 UTF-8이라 기존 고객 파일을 읽으려면 CP949를 명시해야 한다.
	public static class LegacyText
	{
		public static readonly Encoding Encoding;

		static LegacyText()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Encoding = Encoding.GetEncoding(949);
		}
	}
}
```

`BandProgram/Util.cs`에서 `Encoding.Default` 6곳(`createNotePad`, `firstLineToBack`, `readALine`, `readAll`, `readAllToString`, `writeStream`)을 모두 `LegacyText.Encoding`으로 바꾼다.

```bash
sed -i '' 's/Encoding\.Default/LegacyText.Encoding/g' BandProgram/Util.cs
grep -c "LegacyText.Encoding" BandProgram/Util.cs
```
Expected: `6`

- [ ] **Step 5: WinHttp를 HttpClient로 교체**

`BandProgram/Util.cs`에서 `using WinHttp;` 줄을 지우고 `using System.Net.Http;`를 추가한다. 클래스 필드 영역(`private Random rd = new Random();` 아래)에 추가:

```csharp
		private static readonly HttpClient http = new HttpClient();
```

`requestHTTP` 두 메서드를 아래로 바꾼다. WinHttp처럼 HTTP 오류 상태에서도 본문을 돌려준다.

```csharp
		public string requestHTTP(string url)
		{
			using (HttpResponseMessage response = http.GetAsync(url).GetAwaiter().GetResult())
			{
				return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			}
		}

		public string requestHTTP(string url, Dictionary<string, string> parameters)
		{
			string str = "";
			bool flag = true;
			foreach (KeyValuePair<string, string> parameter in parameters)
			{
				if (!flag)
				{
					str = string.Concat(new string[] { str, "&", parameter.Key, "=", parameter.Value });
				}
				else
				{
					str = string.Concat(str, parameter.Key, "=", parameter.Value);
					flag = false;
				}
			}
			return this.requestHTTP(string.Concat(url, "?", str));
		}
```

`using System.Runtime.InteropServices;`가 더 이상 쓰이지 않으면 지운다.

- [ ] **Step 6: 빌드 성공 확인**

Run: `dotnet build BandProgram/BandProgram.csproj 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개"`
Expected: `빌드했습니다.`, `오류 0개`

- [ ] **Step 7: 우클릭 메뉴가 실패하는지 경고로 확인**

Run: `dotnet build BandProgram/BandProgram.csproj --no-incremental -p:WarningsAsErrors=WFDEV006 2>&1 | grep -c "error WFDEV006"`
Expected: 0보다 큰 숫자(`MenuItem`/`ContextMenu` 사용처)

- [ ] **Step 8: `LegacyMenu` 추가**

`BandProgram/LegacyMenu.cs`:

```csharp
using System;
using System.Windows.Forms;

namespace BandProgram
{
	// .NET Framework의 MenuItem/ContextMenu는 .NET 10에서 실행 시 PlatformNotSupportedException을 던진다.
	// 기존 MenuClick 핸들러가 쓰던 MenuItem.Index(구분선 "-" 포함 위치)를 Tag로 보존한다.
	internal static class LegacyMenu
	{
		public static ContextMenuStrip Create(EventHandler onClick, params string[] labels)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			for (int i = 0; i < labels.Length; i++)
			{
				if (labels[i] == "-")
				{
					menu.Items.Add(new ToolStripSeparator());
					continue;
				}
				ToolStripMenuItem item = new ToolStripMenuItem(labels[i]) { Tag = i };
				item.Click += onClick;
				menu.Items.Add(item);
			}
			return menu;
		}

		public static int IndexOf(object sender)
		{
			return (int)((ToolStripItem)sender).Tag;
		}
	}
}
```

- [ ] **Step 9: 메뉴 사용처 6곳 교체**

각 파일에서 아래처럼 바꾼다. 라벨 문자열과 순서는 기존과 똑같이 둔다.

`BandProgram/LoginSecond.cs` `listView1_MouseDown`:
```csharp
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.listView1.ContextMenuStrip = LegacyMenu.Create(eventHandler, "선택된 항목 삭제하기", "전체 삭제하기");
```
`LoginSecond.MenuClick`: `int index = ((MenuItem)obj).Index;` → `int index = LegacyMenu.IndexOf(obj);`

`BandProgram/MainForm.cs` `listView1_MouseDown`:
```csharp
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.listView1.ContextMenuStrip = LegacyMenu.Create(eventHandler, "선택된 항목 삭제하기", "-", "현재 리스트를 파일로 저장하기", "파일로 저장된 리스트 불러오기", "-", "선택된 항목 포스트 번호 일괄 수정하기", "서버에서 가입된 밴드 리스트 불러오기", "-", "모든 항목 선택하기", "모든 항목 선택 해제하기");
```
`MainForm.MenuClick`: `switch (((MenuItem)obj).Index)` → `switch (LegacyMenu.IndexOf(obj))`

`BandProgram/BandLayout.cs` `listView_MouseDown`:
```csharp
				EventHandler eventHandler = new EventHandler(this.MenuClick);
				this.postingListView.ContextMenuStrip = LegacyMenu.Create(eventHandler, "항목 추가하기", "선택된 항목 삭제하기");
```
`BandLayout.MenuClick`: `int index = ((MenuItem)obj).Index;` → `int index = LegacyMenu.IndexOf(obj);`

`BandProgram/NewPostForm.cs` `ImageListView_MouseDown`:
```csharp
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.postImageList.ContextMenuStrip = LegacyMenu.Create(eventHandler, "전체 사진 삭제", "선택한 사진 삭제");
```
`NewPostForm.commentImageList_MouseDown`:
```csharp
                    EventHandler eventHandler = new EventHandler(this.MenuClick_1);
                    this.commentImageList.ContextMenuStrip = LegacyMenu.Create(eventHandler, "전체 사진 삭제", "선택한 사진 삭제");
```
`NewPostForm.MenuClick`와 `MenuClick_1`: `((MenuItem)obj).Index` → `LegacyMenu.IndexOf(obj)`

`BandProgram/PostingAddForm.cs` `ImageListView_MouseDown`:
```csharp
                EventHandler eventHandler = new EventHandler(this.MenuClick);
                this.ImageListView.ContextMenuStrip = LegacyMenu.Create(eventHandler, "전체 사진 삭제", "선택한 사진 삭제");
```
`PostingAddForm.MenuClick`: `((MenuItem)obj).Index` → `LegacyMenu.IndexOf(obj)`

- [ ] **Step 10: `ShellOpen` 추가와 `Process.Start` 6곳 교체**

`BandProgram/ShellOpen.cs`:

```csharp
using System.Diagnostics;

namespace BandProgram
{
	// .NET 10의 Process.Start(string)은 UseShellExecute=false라 폴더·이미지를 열지 못한다.
	internal static class ShellOpen
	{
		public static void Open(string path)
		{
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
	}
}
```

`NewPostForm.ImageListView_DoubleClick`과 `PostingAddForm.ImageListView_DoubleClick`에서 `Process.Start(` 세 번을 모두 `ShellOpen.Open(`으로 바꾼다(인자는 그대로).

```bash
sed -i '' 's/Process\.Start(/ShellOpen.Open(/g' BandProgram/NewPostForm.cs BandProgram/PostingAddForm.cs
grep -c "ShellOpen.Open(" BandProgram/NewPostForm.cs BandProgram/PostingAddForm.cs
```
Expected: 각 파일 `3`

- [ ] **Step 11: 글꼴과 DPI를 기존과 맞추기**

`BandProgram/Program.cs`의 `Main`을 아래로 바꾼다. `using System.Drawing;`을 추가한다.

```csharp
        [STAThread]
        static void Main()
        {
            // .NET Framework와 같은 기본 글꼴·DPI 동작 (.NET Core 3.0부터 기본값이 바뀜)
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());
         //   Application.Run(new NewPostForm());
        }
```

- [ ] **Step 12: 빌드와 경고 확인**

Run: `dotnet build BandProgram/BandProgram.csproj --no-incremental -p:WarningsAsErrors=WFDEV006 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개"`
Expected: `빌드했습니다.`, `오류 0개`

Run: `grep -rn "MenuItem\b\|\.ContextMenu =\|Process\.Start(\|Encoding\.Default\|WinHttp" BandProgram/*.cs | grep -v "ToolStripMenuItem\|LegacyMenu.cs\|ShellOpen.cs"`
Expected: 출력 없음

- [ ] **Step 13: 솔루션 빌드 확인**

Run: `dotnet build BandProgram.sln 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개"`
Expected: `빌드했습니다.`, `오류 0개`

- [ ] **Step 14: Commit**

```bash
git add -A BandProgram
git commit -m "Move BandProgram to SDK-style csproj on .NET 10

Replace WinHttp COM with HttpClient, read/write legacy files as CP949,
replace removed MenuItem/ContextMenu with ContextMenuStrip, open files
with UseShellExecute, and keep the .NET Framework default font and DPI.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: `BandProgram.Core` 분리, 플랫폼 대체, 테스트 프로젝트

**Files:**
- Create: `BandProgram.Core/BandProgram.Core.csproj`
- Move (`git mv`): `BandProgram/{ADB,APIDAO,AccountInfo,AppConfiguration,Band,BandInfo,FunctionList,Global,ImageFile,LegacyText,Naver,NaverMobile,Post,Response,Util}.cs` → `BandProgram.Core/`
- Create: `BandProgram.Core/Platform/AppPaths.cs`, `Platform/IClipboard.cs`, `Platform/PbcopyClipboard.cs`
- Modify: `BandProgram.Core/Util.cs` (클립보드·붙여넣기·chromedata 경로), `FunctionList.cs` (`startPath`, `MessageBox`, `showFileOpenDialog` 제거, private 헬퍼 → internal), `ADB.cs` (실행 파일 이름)
- Create: `BandProgram/WinFormsClipboard.cs`, `BandProgram/ImageFileDialog.cs`
- Modify: `BandProgram/BandProgram.csproj` (Core 참조), `Program.cs` (클립보드), `BandLayout.cs` (`entirePath`), `NewPostForm.cs`·`PostingAddForm.cs` (`ImageFileDialog.Show()`)
- Create: `BandProgram.Tests/` (xUnit) + 테스트 파일 5개
- Modify: `BandProgram.sln`

**Interfaces:**
- Consumes: `LegacyText.Encoding` (Task 2)
- Produces:
  - `AppPaths.DataDir : string` (get/set, set은 `Path.GetFullPath` 적용, 기본 `AppContext.BaseDirectory`)
  - `AppPaths.DataDirWithSlash : string` (`/` 구분자, 끝에 `/` 하나)
  - `interface IClipboard { void SetText(string text); }`, `PbcopyClipboard : IClipboard`
  - `Util.Clipboard : IClipboard` (static get/set, 맥 기본값 `PbcopyClipboard`, 그 외 `null`)
  - `ADB.Executable : string` (internal static, Windows `adb.exe`, 그 외 `adb`)
  - `FunctionList.stringToIntList(string, char) : List<int>`, `FunctionList.intListToString(List<int>) : string` (internal로 변경)
  - `ImageFileDialog.Show() : ImageFile` (UI 전용)
  - Core의 internal 타입은 `BandProgram`, `BandProgram.Dev`, `BandProgram.Tests`에 공개(`InternalsVisibleTo`)

- [ ] **Step 1: Core 프로젝트 만들고 파일 옮기기**

```bash
mkdir -p BandProgram.Core/Platform
git mv BandProgram/ADB.cs BandProgram/APIDAO.cs BandProgram/AccountInfo.cs BandProgram/AppConfiguration.cs \
       BandProgram/Band.cs BandProgram/BandInfo.cs BandProgram/FunctionList.cs BandProgram/Global.cs \
       BandProgram/ImageFile.cs BandProgram/LegacyText.cs BandProgram/Naver.cs BandProgram/NaverMobile.cs \
       BandProgram/Post.cs BandProgram/Response.cs BandProgram/Util.cs BandProgram.Core/
```

`BandProgram.Core/BandProgram.Core.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>BandProgram</RootNamespace>
    <AssemblyName>BandProgram.Core</AssemblyName>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="BandProgram" />
    <InternalsVisibleTo Include="BandProgram.Dev" />
    <InternalsVisibleTo Include="BandProgram.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Selenium.WebDriver" Version="3.141.0" />
    <PackageReference Include="Selenium.Support" Version="3.141.0" />
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="10.0.0" />
  </ItemGroup>

</Project>
```

`BandProgram/BandProgram.csproj`의 패키지 `ItemGroup`을 Core 참조로 바꾼다(패키지는 Core에서 전이됨).

```xml
  <ItemGroup>
    <ProjectReference Include="..\BandProgram.Core\BandProgram.Core.csproj" />
  </ItemGroup>
```

```bash
dotnet sln BandProgram.sln add BandProgram.Core/BandProgram.Core.csproj
```

- [ ] **Step 2: Core 빌드 오류 확인**

Run: `dotnet build BandProgram.Core 2>&1 | grep -E " error " | sed -E 's/ \[.*\]//; s#.*/BandProgram.Core/##' | sort -u`
Expected: `System.Windows.Forms` 관련 오류(`FunctionList.cs`, `Util.cs`의 `using`, `Application`, `MessageBox`, `OpenFileDialog`, `Clipboard`)

- [ ] **Step 3: 테스트 프로젝트 만들기**

```bash
dotnet new xunit -o BandProgram.Tests -f net10.0
rm BandProgram.Tests/UnitTest1.cs
dotnet add BandProgram.Tests reference BandProgram.Core/BandProgram.Core.csproj
dotnet sln BandProgram.sln add BandProgram.Tests/BandProgram.Tests.csproj
```

`BandProgram.Tests/BandProgram.Tests.csproj`의 `PropertyGroup`에 아래를 추가한다(이 프로젝트에만 쓰는 using).

```xml
    <RootNamespace>BandProgram.Tests</RootNamespace>
```

작업 디렉터리를 바꾸는 테스트끼리 동시에 돌지 않게 `BandProgram.Tests/SerialCollection.cs`:

```csharp
namespace BandProgram.Tests;

// 현재 디렉터리, AppPaths, Util 싱글톤 같은 전역 상태를 바꾸는 테스트를 묶어 순서대로 실행한다.
[CollectionDefinition("Serial", DisableParallelization = true)]
public class SerialCollection
{
}
```

- [ ] **Step 4: 실패하는 테스트 작성 — AppPaths, LegacyText, 파싱 헬퍼, 포스팅 폴더, 플랫폼**

`BandProgram.Tests/AppPathsTests.cs`:

```csharp
namespace BandProgram.Tests;

[Collection("Serial")]
public class AppPathsTests
{
    [Fact]
    public void DataDirWithSlash_uses_forward_slashes_and_one_trailing_slash()
    {
        string original = AppPaths.DataDir;
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "band-apppaths");
            AppPaths.DataDir = dir + Path.DirectorySeparatorChar;
            Assert.Equal(Path.GetFullPath(dir).Replace('\\', '/').TrimEnd('/') + "/", AppPaths.DataDirWithSlash);
            Assert.False(AppPaths.DataDirWithSlash.EndsWith("//"));
        }
        finally
        {
            AppPaths.DataDir = original;
        }
    }

    [Fact]
    public void DataDir_defaults_to_app_base_directory()
    {
        Assert.Equal(Path.GetFullPath(AppContext.BaseDirectory), Path.GetFullPath(AppPaths.DataDir));
    }
}
```

`BandProgram.Tests/LegacyTextTests.cs`:

```csharp
using System.Text;

namespace BandProgram.Tests;

[Collection("Serial")]
public class LegacyTextTests
{
    private static readonly byte[] Cp949Ga = { 0xB0, 0xA1 }; // "가"

    [Fact]
    public void Encoding_is_cp949()
    {
        Assert.Equal(949, LegacyText.Encoding.CodePage);
        Assert.Equal("가", LegacyText.Encoding.GetString(Cp949Ga));
    }

    [Fact]
    public void Util_reads_existing_cp949_files_without_mojibake()
    {
        string file = Path.Combine(Path.GetTempPath(), $"band-legacy-{Guid.NewGuid():N}.txt");
        File.WriteAllBytes(file, LegacyText.Encoding.GetBytes("12345\t캠핑 밴드\r\n67890\t낚시\r\n"));
        try
        {
            List<string> lines = Util.getInstance().readAll(file);
            Assert.Equal(new[] { "12345\t캠핑 밴드", "67890\t낚시" }, lines);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Util_writes_cp949_so_the_old_windows_app_can_read_it()
    {
        string file = Path.Combine(Path.GetTempPath(), $"band-legacy-{Guid.NewGuid():N}.txt");
        try
        {
            Util.getInstance().writeStream(file, "가");
            byte[] bytes = File.ReadAllBytes(file);
            Assert.Equal(Cp949Ga.Concat(Encoding.ASCII.GetBytes(Environment.NewLine)).ToArray(), bytes);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
```

`BandProgram.Tests/ParsingTests.cs`:

```csharp
namespace BandProgram.Tests;

[Collection("Serial")]
public class ParsingTests
{
    [Theory]
    [InlineData("30초", 30)]
    [InlineData("2분", 120)]
    [InlineData("1시간", 3600)]
    [InlineData("무한", 0)]
    public void calculateTime_converts_korean_units_to_seconds(string text, int expected)
    {
        Assert.Equal(expected, Util.getInstance().calculateTime(text));
    }

    [Fact]
    public void stringToIntList_skips_non_numbers()
    {
        Assert.Equal(new List<int> { 1, 2, 3 }, new FunctionList().stringToIntList("1,2,x,3", ','));
    }

    [Fact]
    public void stringToIntList_returns_null_for_null()
    {
        Assert.Null(new FunctionList().stringToIntList(null, ','));
    }

    [Fact]
    public void intListToString_joins_with_comma_space()
    {
        Assert.Equal("1, 2, 3", new FunctionList().intListToString(new List<int> { 1, 2, 3 }));
        Assert.Equal("", new FunctionList().intListToString(null));
    }
}
```

`BandProgram.Tests/PostingFolderTests.cs`:

```csharp
namespace BandProgram.Tests;

[Collection("Serial")]
public class PostingFolderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"band-posting-{Guid.NewGuid():N}");
    private readonly string originalCwd = Directory.GetCurrentDirectory();
    private readonly string originalDataDir = AppPaths.DataDir;

    public PostingFolderTests()
    {
        string post1 = Path.Combine(root, "AutoDoc", "Posting", "post_1");
        string post3 = Path.Combine(root, "AutoDoc", "Posting", "post_3");
        Directory.CreateDirectory(post1);
        Directory.CreateDirectory(post3);
        File.WriteAllBytes(Path.Combine(post1, "contents.txt"), LegacyText.Encoding.GetBytes("=가나다콜=\r\n전국 어디서나"));
        File.WriteAllBytes(Path.Combine(post1, "comment_contents.txt"), LegacyText.Encoding.GetBytes("댓글입니다"));
        File.WriteAllBytes(Path.Combine(post1, "a.png"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(post3, "contents.txt"), LegacyText.Encoding.GetBytes("세번째"));

        // 기존 코드는 폴더 목록은 현재 디렉터리 기준, 파일 내용은 실행 폴더(startPath) 기준으로 읽는다.
        Directory.SetCurrentDirectory(root);
        AppPaths.DataDir = root;
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(originalCwd);
        AppPaths.DataDir = originalDataDir;
        Directory.Delete(root, true);
    }

    [Fact]
    public void getPostingList_reads_cp949_contents_images_and_comment()
    {
        List<Post> posts = new FunctionList().getPostingList("AutoDoc/Posting", "post_");

        Assert.NotNull(posts);
        Post first = posts.Single(p => p.idx == 1);
        Assert.Equal("=가나다콜=" + Environment.NewLine + "전국 어디서나" + Environment.NewLine, first.contents);
        Assert.Single(first.images);
        Assert.Equal("a.png", first.images[0].getFileName());
        Assert.True(first.has_comment);
        Assert.Equal("댓글입니다" + Environment.NewLine, first.comment_contents);

        Post third = posts.Single(p => p.idx == 3);
        Assert.False(third.has_comment);
    }

    [Fact]
    public void getPostingNum_returns_first_gap()
    {
        Assert.Equal(2, new FunctionList().getPostingNum("AutoDoc/Posting", "post_"));
    }
}
```

`Post`의 필드 이름(`idx`, `contents`, `images`, `comment_contents`, `has_comment`)과 `ImageFile.getFileName()`은 기존 코드 그대로다. `BandProgram.Core/Post.cs`에서 `images`가 `List<ImageFile>`인지 확인하고, 다르면 테스트의 인덱싱만 맞춘다.

`BandProgram.Tests/PlatformTests.cs`:

```csharp
using System.Diagnostics;

namespace BandProgram.Tests;

[Collection("Serial")]
public class PlatformTests
{
    [Fact]
    public void Adb_executable_matches_os()
    {
        Assert.Equal(OperatingSystem.IsWindows() ? "adb.exe" : "adb", ADB.Executable);
    }

    [Fact]
    public void Util_clipboard_defaults_to_pbcopy_on_mac()
    {
        if (!OperatingSystem.IsMacOS()) return;
        Assert.IsType<PbcopyClipboard>(Util.Clipboard);
    }

    [Fact]
    public void Pbcopy_clipboard_keeps_korean_and_newlines()
    {
        if (!OperatingSystem.IsMacOS()) return;
        string previous = RunPbpaste();
        try
        {
            new PbcopyClipboard().SetText("한글 붙여넣기\n둘째 줄");
            Assert.Equal("한글 붙여넣기\n둘째 줄", RunPbpaste());
        }
        finally
        {
            new PbcopyClipboard().SetText(previous); // 사용자의 클립보드 복원
        }
    }

    private static string RunPbpaste()
    {
        var psi = new ProcessStartInfo("pbpaste") { RedirectStandardOutput = true };
        psi.Environment["LANG"] = "en_US.UTF-8";
        using Process p = Process.Start(psi)!;
        string text = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return text;
    }
}
```

- [ ] **Step 5: 테스트가 컴파일 실패하는지 확인**

Run: `dotnet test BandProgram.Tests 2>&1 | grep -E " error " | sed -E 's/ \[.*\]//' | sort -u | head`
Expected: `AppPaths`, `PbcopyClipboard`, `ADB.Executable`, `Util.Clipboard`를 찾을 수 없다는 오류와 Core의 WinForms 오류

- [ ] **Step 6: 플랫폼 클래스 추가**

`BandProgram.Core/Platform/AppPaths.cs`:

```csharp
using System;
using System.IO;

namespace BandProgram
{
	// 기존 Application.StartupPath 자리. 고객 앱은 exe 폴더, Dev 셸은 --data 폴더를 쓴다.
	public static class AppPaths
	{
		private static string dataDir = Path.GetFullPath(AppContext.BaseDirectory);

		public static string DataDir
		{
			get { return dataDir; }
			set { dataDir = Path.GetFullPath(value); }
		}

		// 기존 코드의 startPath 형식: '/' 구분자, 끝에 '/'
		public static string DataDirWithSlash
		{
			get { return string.Concat(dataDir.Replace('\\', '/').TrimEnd('/'), "/"); }
		}
	}
}
```

`BandProgram.Core/Platform/IClipboard.cs`:

```csharp
namespace BandProgram
{
	public interface IClipboard
	{
		void SetText(string text);
	}
}
```

`BandProgram.Core/Platform/PbcopyClipboard.cs`:

```csharp
using System.Diagnostics;

namespace BandProgram
{
	public sealed class PbcopyClipboard : IClipboard
	{
		public void SetText(string text)
		{
			ProcessStartInfo psi = new ProcessStartInfo("pbcopy")
			{
				RedirectStandardInput = true,
				UseShellExecute = false
			};
			// LANG이 없으면 pbcopy가 입력을 UTF-8로 해석하지 않아 한글이 들어가지 않는다.
			psi.Environment["LANG"] = "en_US.UTF-8";
			using (Process process = Process.Start(psi))
			{
				process.StandardInput.Write(text);
				process.StandardInput.Close();
				process.WaitForExit();
			}
		}
	}
}
```

- [ ] **Step 7: `Util`에서 WinForms 제거**

`BandProgram.Core/Util.cs`:

1. `using System.Windows.Forms;`를 지운다. `using OpenQA.Selenium.Interactions;`는 이미 있다.
2. 필드 영역에 추가:

```csharp
		// Windows는 WinForms Program이 WinFormsClipboard를 넣는다.
		public static IClipboard Clipboard { get; set; } = OperatingSystem.IsMacOS() ? new PbcopyClipboard() : null;
```

3. `sendKeyPaste` 전체를 아래로 바꾼다. Windows 경로는 기존과 같다. 맥은 `element.SendKeys(Command + "v")`가 동작하지 않아(시험으로 확인) 포커스를 준 뒤 `Actions`로 누른다.

```csharp
		public bool sendKeyPaste(string css, string msg)
		{
			bool flag;
			try
			{
				Util.Clipboard.SetText(msg);
				this.delay(500);
				if (OperatingSystem.IsMacOS())
				{
					IWebElement element = this.findElement(css);
					((IJavaScriptExecutor)this.driver).ExecuteScript("arguments[0].focus();", new object[] { element });
					new Actions(this.driver).KeyDown(OpenQA.Selenium.Keys.Command).SendKeys("v").KeyUp(OpenQA.Selenium.Keys.Command).Perform();
					this.delay(100);
				}
				else
				{
					this.findElement(css).SendKeys(string.Concat(OpenQA.Selenium.Keys.LeftControl, "v"));
					this.delay(100);
					this.findElement(css).SendKeys(OpenQA.Selenium.Keys.LeftControl);
					this.delay(100);
				}
				flag = true;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}
```

4. `startChrome`의 경로 두 줄을 바꾼다.

```csharp
                chromeOption.AddArgument("user-data-dir=" + Path.Combine(AppPaths.DataDir, "chromedata"));
```
```csharp
				chromeOption.AddArgument("--disk-cache-dir=" + Path.Combine(AppPaths.DataDir, "Cache"));
```

- [ ] **Step 8: `FunctionList`에서 WinForms 제거**

`BandProgram.Core/FunctionList.cs`:

1. `using System.Windows.Forms;`를 지운다.
2. `private string startPath = string.Concat(Application.StartupPath.Replace('\\', '/'), "/");`를 아래로 바꾼다(사용처 13곳은 그대로).

```csharp
		private string startPath
		{
			get { return AppPaths.DataDirWithSlash; }
		}
```

3. `getBandInfoFromUrl` 안의 `MessageBox.Show(str1);` 줄을 지운다(디버그 팝업 제거, 스펙 4장).
4. `showFileOpenDialog` 메서드 전체를 지운다(Step 10에서 UI로 옮김).
5. `private string intListToString(`과 `private List<int> stringToIntList(`의 `private`을 `internal`로 바꾼다.

- [ ] **Step 9: `ADB` 실행 파일 이름**

`BandProgram.Core/ADB.cs`에 추가하고 `executeADB`에서 사용한다. 예외는 기존처럼 호출부로 전파한다(스펙 4장: 삼키면 `changeIP`가 무한 재귀).

```csharp
		internal static string Executable
		{
			get { return OperatingSystem.IsWindows() ? "adb.exe" : "adb"; }
		}
```
```csharp
			this.startInfo.FileName = ADB.Executable;
```

- [ ] **Step 10: UI 쪽 대체 클래스 추가와 호출부 변경**

`BandProgram/WinFormsClipboard.cs` (기존 `sendKeyPaste`의 STA 스레드 방식 그대로):

```csharp
using System.Threading;
using System.Windows.Forms;

namespace BandProgram
{
	internal sealed class WinFormsClipboard : IClipboard
	{
		public void SetText(string text)
		{
			Thread thread = new Thread(() => Clipboard.SetText(text));
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
			thread.Join();
		}
	}
}
```

`BandProgram/ImageFileDialog.cs` (기존 `FunctionList.showFileOpenDialog` 그대로):

```csharp
using System.IO;
using System.Windows.Forms;

namespace BandProgram
{
	internal static class ImageFileDialog
	{
		public static ImageFile Show()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog()
			{
				Title = "이미지 파일 불러오기",
				FileName = "",
				Filter = "그림 파일 (*.jpg, *.jpeg, *.gif, *.bmp, *.png) | *.jpg; *.jpeg; *.gif; *.bmp; *.png;"
			};
			DialogResult dialogResult = openFileDialog.ShowDialog();
			if (dialogResult != DialogResult.OK)
			{
				return null;
			}
			string safeFileName = openFileDialog.SafeFileName;
			string fileName = openFileDialog.FileName;
			string str = fileName.Replace(safeFileName, "");
			return new ImageFile(safeFileName, str, (new FileInfo(fileName)).Length);
		}
	}
}
```

호출부 3곳(`NewPostForm.cs` 2곳, `PostingAddForm.cs` 1곳)의 `this.fl.showFileOpenDialog()`를 `ImageFileDialog.Show()`로 바꾼다.

```bash
sed -i '' 's/this\.fl\.showFileOpenDialog()/ImageFileDialog.Show()/g' BandProgram/NewPostForm.cs BandProgram/PostingAddForm.cs
```

`BandProgram/Program.cs`의 `Main` 첫 줄에 추가:

```csharp
            Util.Clipboard = new WinFormsClipboard();
```

`BandProgram/BandLayout.cs` 생성자의 `entirePath` 줄을 바꾼다(기존과 같은 `.../path/sep` 형식).

```csharp
			this.entirePath = string.Concat(new string[] { AppPaths.DataDirWithSlash, path, "/", sep });
```

- [ ] **Step 11: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests 2>&1 | tail -3`
Expected: `통과!` 또는 `Passed!`, 실패 0

- [ ] **Step 12: 솔루션 빌드와 WinForms 잔재 확인**

Run: `dotnet build BandProgram.sln -p:WarningsAsErrors=WFDEV006 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개"`
Expected: `빌드했습니다.`, `오류 0개`

Run: `grep -rn "System.Windows.Forms\|Application\.StartupPath\|MessageBox" BandProgram.Core`
Expected: 출력 없음

- [ ] **Step 13: Commit**

```bash
git add -A BandProgram BandProgram.Core BandProgram.Tests BandProgram.sln
git commit -m "Split automation logic into cross-platform BandProgram.Core

Add AppPaths, IClipboard with pbcopy for macOS, OS-specific paste and adb
names, move the image file dialog into the WinForms app, and add an xUnit
project covering CP949 files, posting folders, and parsing helpers.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: `WorkControl` (일시정지·재개·초기화)

**Files:**
- Create: `BandProgram.Core/Work/WorkControl.cs`
- Modify: `BandProgram.Core/Util.cs` (`delay`)
- Modify: `BandProgram/BandLayout.cs`, `BandProgram/MainForm.cs`
- Modify: `BandProgram/BandProgram.csproj` (실행 시 실패하는 API 경고를 오류로)
- Test: `BandProgram.Tests/WorkControlTests.cs`

**Interfaces:**
- Consumes: `Util.delay(int)` (기존)
- Produces:
  - `sealed class WorkControl` with `Thread Start(Action work)`, `void Pause()`, `void Resume()`, `void Reset()`, `bool IsPaused { get; }`, `static void Checkpoint()`
  - `internal const int SliceMs = 200`
  - `Util.delay(int MS)`는 등록된 작업 스레드에서 최대 `SliceMs` 단위로 자며 매번 `Checkpoint()`를 호출

- [ ] **Step 1: 실패하는 테스트 작성**

`BandProgram.Tests/WorkControlTests.cs`:

```csharp
namespace BandProgram.Tests;

[Collection("Serial")]
public class WorkControlTests
{
    // 작업 스레드가 확인 지점을 계속 지나가며 카운터를 올린다.
    private static Thread StartCounter(WorkControl control, Counter counter)
    {
        return control.Start(() =>
        {
            while (true)
            {
                counter.Increment();
                WorkControl.Checkpoint();
                Thread.Sleep(5);
            }
        });
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 3000)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > end) throw new TimeoutException("condition not met");
            Thread.Sleep(10);
        }
    }

    private static bool IsFrozen(Counter counter)
    {
        Thread.Sleep(100); // 확인 지점에 도달할 시간
        int before = counter.Value;
        Thread.Sleep(300);
        return counter.Value == before;
    }

    [Fact]
    public void Pause_blocks_and_Resume_continues()
    {
        var control = new WorkControl();
        var counter = new Counter();
        StartCounter(control, counter);
        WaitUntil(() => counter.Value > 3);

        control.Pause();
        Assert.True(control.IsPaused);
        Assert.True(IsFrozen(counter));

        control.Resume();
        int afterResume = counter.Value;
        WaitUntil(() => counter.Value > afterResume + 3);
        control.Reset();
    }

    [Fact]
    public void Reset_parks_old_work_and_new_work_runs()
    {
        var control = new WorkControl();
        var oldCounter = new Counter();
        StartCounter(control, oldCounter);
        WaitUntil(() => oldCounter.Value > 3);

        control.Reset();
        Assert.True(IsFrozen(oldCounter));

        var newCounter = new Counter();
        StartCounter(control, newCounter);
        WaitUntil(() => newCounter.Value > 3);
        Assert.True(IsFrozen(oldCounter));
        control.Reset();
    }

    [Fact]
    public void Reset_while_paused_parks_old_work()
    {
        var control = new WorkControl();
        var counter = new Counter();
        StartCounter(control, counter);
        WaitUntil(() => counter.Value > 3);

        control.Pause();
        control.Reset();
        Assert.False(control.IsPaused);
        Assert.True(IsFrozen(counter));
    }

    [Fact]
    public void Instances_are_independent()
    {
        // 포스팅을 일시정지한 채 댓글을 시작해도, 재개하면 포스팅이 이어져야 한다(기존 동작).
        var posting = new WorkControl();
        var comment = new WorkControl();
        var postingCounter = new Counter();
        var commentCounter = new Counter();
        StartCounter(posting, postingCounter);
        WaitUntil(() => postingCounter.Value > 3);

        posting.Pause();
        StartCounter(comment, commentCounter);
        WaitUntil(() => commentCounter.Value > 3);
        Assert.True(IsFrozen(postingCounter));

        posting.Resume();
        int afterResume = postingCounter.Value;
        WaitUntil(() => postingCounter.Value > afterResume + 3);
        posting.Reset();
        comment.Reset();
    }

    [Fact]
    public void Unregistered_thread_is_not_affected()
    {
        var control = new WorkControl();
        control.Pause();
        var task = Task.Run(() =>
        {
            WorkControl.Checkpoint();
            Util.getInstance().delay(50);
        });
        Assert.True(task.Wait(2000));
        control.Reset();
    }

    [Fact]
    public void Pause_inside_long_delay_stops_promptly()
    {
        var control = new WorkControl();
        bool finished = false;
        control.Start(() =>
        {
            Util.getInstance().delay(600);
            finished = true;
        });
        Thread.Sleep(50);
        control.Pause();
        Thread.Sleep(1000); // delay(600)이 끝났을 시간
        Assert.False(finished);

        control.Resume();
        WaitUntil(() => finished);
        control.Reset();
    }

    [Fact]
    public void Delay_still_waits_the_requested_time()
    {
        var control = new WorkControl();
        long elapsed = 0;
        Thread t = control.Start(() =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Util.getInstance().delay(450);
            elapsed = sw.ElapsedMilliseconds;
        });
        Assert.True(t.Join(3000));
        Assert.InRange(elapsed, 440, 1500);
    }

    [Fact]
    public void Started_threads_are_background()
    {
        var control = new WorkControl();
        Thread t = control.Start(() => { });
        Assert.True(t.IsBackground);
        t.Join();
    }

    private sealed class Counter
    {
        private int value;
        public int Value => Volatile.Read(ref value);
        public void Increment() => Interlocked.Increment(ref value);
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `dotnet test BandProgram.Tests --filter WorkControlTests 2>&1 | grep -E " error " | head -3`
Expected: `WorkControl` 형식을 찾을 수 없음

- [ ] **Step 3: `WorkControl` 구현**

`BandProgram.Core/Work/WorkControl.cs`:

```csharp
using System;
using System.Threading;

namespace BandProgram
{
	// Thread.Suspend/Resume/Abort 대체. 작업 스레드는 Util.delay()의 확인 지점에서 멈춘다.
	// 작업 종류(포스팅/댓글/채팅/가입)마다 인스턴스를 하나씩 둔다.
	public sealed class WorkControl
	{
		internal const int SliceMs = 200;

		[ThreadStatic]
		private static WorkControl current;

		[ThreadStatic]
		private static int currentGeneration;

		private readonly object sync = new object();
		private readonly ManualResetEventSlim gate = new ManualResetEventSlim(true);
		private int generation;

		public bool IsPaused
		{
			get { return !this.gate.IsSet; }
		}

		public Thread Start(Action work)
		{
			int gen;
			lock (this.sync)
			{
				this.generation++;
				gen = this.generation;
				this.gate.Set();
			}
			Thread thread = new Thread(() =>
			{
				current = this;
				currentGeneration = gen;
				work();
			});
			thread.IsBackground = true;
			thread.Start();
			return thread;
		}

		public void Pause()
		{
			this.gate.Reset();
		}

		public void Resume()
		{
			this.gate.Set();
		}

		// 기존 Abort 자리. 진행 중인 작업은 다음 확인 지점에서 영구히 멈춘다.
		// 예외를 던지지 않는 이유: 흐름 코드의 catch {}가 삼키고 다음 단계를 실행할 수 있다.
		public void Reset()
		{
			lock (this.sync)
			{
				this.generation++;
				this.gate.Set();
			}
		}

		public static void Checkpoint()
		{
			WorkControl control = current;
			if (control == null)
			{
				return;
			}
			control.Wait(currentGeneration);
		}

		private void Wait(int gen)
		{
			while (true)
			{
				if (gen != Volatile.Read(ref this.generation))
				{
					Thread.Sleep(Timeout.Infinite);
				}
				this.gate.Wait();
				if (gen == Volatile.Read(ref this.generation))
				{
					return;
				}
			}
		}
	}
}
```

- [ ] **Step 4: `Util.delay`에 확인 지점 넣기**

`BandProgram.Core/Util.cs`의 `delay`를 바꾼다.

```csharp
		public DateTime delay(int MS)
		{
			WorkControl.Checkpoint();
			int remaining = MS;
			while (remaining > 0)
			{
				int slice = Math.Min(remaining, WorkControl.SliceMs);
				Thread.Sleep(slice);
				remaining -= slice;
				WorkControl.Checkpoint();
			}
			return DateTime.Now;
		}
```

- [ ] **Step 5: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests --filter WorkControlTests 2>&1 | tail -3`
Expected: 통과, 실패 0

- [ ] **Step 6: `BandLayout` 교체**

`BandProgram/BandLayout.cs`:

필드 `private Thread thPosting;` 아래에 추가:

```csharp
		private readonly WorkControl work = new WorkControl();

		private bool isPaused;

		private bool isReset;
```

`anotherRunning`:

```csharp
		public void anotherRunning(bool isRunning)
		{
			this.btnStart.Enabled = !isRunning;
			this.btnPause.Enabled = false;
			if (this.thPosting != null && !this.isReset)
			{
				this.btnInit.Enabled = true;
				return;
			}
			this.btnInit.Enabled = false;
		}
```

`btnInit_Click`:

```csharp
		private void btnInit_Click(object sender, EventArgs e)
		{
			this.work.Reset();
			this.isReset = true;
			this.isPaused = false;
			this.toggleState(true, true);
		}
```

`btnPause_Click`:

```csharp
		private void btnPause_Click(object sender, EventArgs e)
		{
			if (this.thPosting == null)
			{
				return;
			}
			this.work.Pause();
			this.isPaused = true;
			this.toggleState(true, false);
		}
```

`btnStart_Click`: 조건 `this.thPosting == null || this.thPosting.ThreadState != ThreadState.Suspended`를 `this.thPosting == null || !this.isPaused`로 바꾸고, 두 갈래의 스레드 코드를 바꾼다(로그 문구는 그대로).

```csharp
				this.isReset = false;
				this.thPosting = this.work.Start(this.startWork);
```
```csharp
				this.resume();
				this.isPaused = false;
				this.work.Resume();
```

- [ ] **Step 7: `MainForm` 가입 작업 교체**

`BandProgram/MainForm.cs`: `private Thread thSignup;` 아래에 추가:

```csharp
        private readonly WorkControl signupWork = new WorkControl();
        private bool signupPaused;
```

`button15_Click`:

```csharp
        private void button15_Click(object sender, EventArgs e)
        {
            if (this.thSignup == null || !this.signupPaused)
            {
                this.printLog(string.Concat(this.userid, " -> 가입 작업 시작"));
                this.thSignup = this.signupWork.Start(this.signupBand);
            }
            else
            {
                this.printLog("가입 작업 재시작");
                if (!this.setBandMain())
                {
                    return;
                }
                this.signupPaused = false;
                this.signupWork.Resume();
            }
            this.toggleToBandSign(false, false);
        }
```

`button14_Click`:

```csharp
        private void button14_Click(object sender, EventArgs e)
        {
            if (this.thSignup == null)
            {
                return;
            }
            this.signupWork.Pause();
            this.signupPaused = true;
            this.toggleToBandSign(true, false);
        }
```

`button10_Click`:

```csharp
        private void button10_Click(object sender, EventArgs e)
        {
            this.signupWork.Reset();
            this.signupPaused = false;
            this.toggleToBandSign(true, true);
        }
```

- [ ] **Step 8: 실행 시 실패하는 API를 빌드 오류로 막기**

`BandProgram/BandProgram.csproj`와 `BandProgram.Core/BandProgram.Core.csproj`의 `PropertyGroup`에 추가:

```xml
    <!-- .NET 10에서 실행 시 PlatformNotSupportedException을 던지는 API -->
    <WarningsAsErrors>WFDEV006;SYSLIB0006;CS0618</WarningsAsErrors>
```

Run: `dotnet build BandProgram.sln --no-incremental 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개"`
Expected: `빌드했습니다.`, `오류 0개`

Run: `grep -rn "\.Suspend()\|\.Resume()\|\.Abort()\|ThreadState\." BandProgram/*.cs | grep -v "SuspendLayout\|ResumeLayout\|work\.Resume\|signupWork\.Resume"`
Expected: 출력 없음

- [ ] **Step 9: 전체 테스트**

Run: `dotnet test BandProgram.Tests 2>&1 | tail -3`
Expected: 통과, 실패 0

- [ ] **Step 10: Commit**

```bash
git add -A BandProgram BandProgram.Core BandProgram.Tests
git commit -m "Replace Thread.Suspend/Resume/Abort with WorkControl checkpoints

Work threads now pause and reset at Util.delay() checkpoints, one control
per job type, so pause/resume/init keep working on .NET 10.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: `SelectorTrace` (셀렉터 실패 추적)

**Files:**
- Create: `BandProgram.Core/Diagnostics/SelectorTrace.cs`
- Modify: `BandProgram.Core/Util.cs` (`findElement` 2개, `findElements`, `findElementsWithXPath`)
- Modify: `BandProgram/Program.cs` (WinForms 싱크: `selector-miss.log`)
- Test: `BandProgram.Tests/SelectorTraceTests.cs`

**Interfaces:**
- Consumes: `AppPaths.DataDir` (Task 3), `Util.getDriver()` (기존)
- Produces:
  - `static class SelectorTrace` with `Action<string> Sink`, `bool SnapshotEnabled`, `void Miss(string selector, IWebDriver driver)`, `string SaveSnapshot(IWebDriver driver)` (저장 폴더 경로 또는 실패 시 `null`)
  - `internal static Func<DateTime> Now`, `internal static void ResetForTests()`
  - 출력 형식:
    ```
    [SELECTOR MISS] "<selector>"
      ← <Type>.<Method> (<File>.cs:<Line>)
      url: <url | (no driver) | (url unavailable)>
      snapshot: <dir>          ← SnapshotEnabled이고 저장에 성공했을 때만
    ```

- [ ] **Step 1: 실패하는 테스트 작성**

`BandProgram.Tests/SelectorTraceTests.cs`:

```csharp
using System.Runtime.CompilerServices;

namespace BandProgram.Tests;

[Collection("Serial")]
public class SelectorTraceTests : IDisposable
{
    private readonly List<string> lines = new();
    private DateTime now = new DateTime(2026, 10, 7, 15, 30, 12);

    public SelectorTraceTests()
    {
        SelectorTrace.ResetForTests();
        SelectorTrace.Sink = line => lines.Add(line);
        SelectorTrace.SnapshotEnabled = false;
        SelectorTrace.Now = () => now;
    }

    public void Dispose()
    {
        SelectorTrace.ResetForTests();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LookUpMissingElement()
    {
        Util.getInstance().findElement("[class='result _bandPageCount']"); // 드라이버 없음 → 실패
    }

    [Fact]
    public void Miss_through_Util_reports_selector_and_first_caller_outside_Util()
    {
        LookUpMissingElement();

        string line = Assert.Single(lines);
        Assert.Contains("[SELECTOR MISS] \"[class='result _bandPageCount']\"", line);
        Assert.Contains("← SelectorTraceTests.LookUpMissingElement (SelectorTraceTests.cs:", line);
        Assert.DoesNotContain("Util.", line);
    }

    [Fact]
    public void Miss_without_driver_still_logs_and_never_throws()
    {
        SelectorTrace.SnapshotEnabled = true; // 드라이버가 없으니 스냅샷은 건너뛴다
        SelectorTrace.Miss(".x", null);

        string line = Assert.Single(lines);
        Assert.Contains("url: (no driver)", line);
        Assert.DoesNotContain("snapshot:", line);
    }

    [Fact]
    public void Same_location_within_one_second_is_logged_once()
    {
        LookUpMissingElement();
        now = now.AddMilliseconds(500);
        LookUpMissingElement();
        Assert.Single(lines);

        now = now.AddMilliseconds(600);
        LookUpMissingElement();
        Assert.Equal(2, lines.Count);
    }

    [Fact]
    public void No_sink_means_no_work()
    {
        SelectorTrace.Sink = null;
        LookUpMissingElement(); // 예외 없이 끝나야 한다
        Assert.Empty(lines);
    }

    [Fact]
    public void Throwing_sink_does_not_escape()
    {
        SelectorTrace.Sink = _ => throw new InvalidOperationException("boom");
        LookUpMissingElement();
    }
}
```

- [ ] **Step 2: 실패 확인**

Run: `dotnet test BandProgram.Tests --filter SelectorTraceTests 2>&1 | grep -E " error " | head -3`
Expected: `SelectorTrace`를 찾을 수 없음

- [ ] **Step 3: `SelectorTrace` 구현**

`BandProgram.Core/Diagnostics/SelectorTrace.cs`:

```csharp
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BandProgram
{
	// Util.findElement* 가 요소를 못 찾았을 때 셀렉터와 호출 위치(Util 바깥 첫 프레임)를 기록한다.
	// 기록만 하고 반환값·예외 흐름은 바꾸지 않는다.
	public static class SelectorTrace
	{
		public static Action<string> Sink;

		public static bool SnapshotEnabled;

		internal static Func<DateTime> Now = () => DateTime.Now;

		private static readonly object sync = new object();

		private static readonly Dictionary<string, DateTime> lastLogged = new Dictionary<string, DateTime>();

		public static void Miss(string selector, IWebDriver driver)
		{
			Action<string> sink = Sink;
			if (sink == null)
			{
				return;
			}
			try
			{
				string caller = FindCaller(new StackTrace(1, true));
				DateTime now = Now();
				string key = string.Concat(selector, "|", caller);
				lock (sync)
				{
					DateTime last;
					if (lastLogged.TryGetValue(key, out last) && now - last < TimeSpan.FromSeconds(1))
					{
						return;
					}
					lastLogged[key] = now;
				}
				string snapshot = SnapshotEnabled ? SaveSnapshot(driver) : null;
				string text = string.Concat(
					"[SELECTOR MISS] \"", selector, "\"", Environment.NewLine,
					"  ← ", caller, Environment.NewLine,
					"  url: ", SafeUrl(driver));
				if (snapshot != null)
				{
					text = string.Concat(text, Environment.NewLine, "  snapshot: ", snapshot);
				}
				sink(text);
			}
			catch
			{
			}
		}

		public static string SaveSnapshot(IWebDriver driver)
		{
			if (driver == null)
			{
				return null;
			}
			try
			{
				string dir = Path.Combine(AppPaths.DataDir, "failures", Now().ToString("yyyyMMdd-HHmmss-fff"));
				Directory.CreateDirectory(dir);
				try
				{
					File.WriteAllText(Path.Combine(dir, "page.html"), driver.PageSource);
				}
				catch
				{
				}
				try
				{
					File.WriteAllBytes(Path.Combine(dir, "screen.png"), ((ITakesScreenshot)driver).GetScreenshot().AsByteArray);
				}
				catch
				{
				}
				return dir;
			}
			catch
			{
				return null;
			}
		}

		internal static void ResetForTests()
		{
			Sink = null;
			SnapshotEnabled = false;
			Now = () => DateTime.Now;
			lock (sync)
			{
				lastLogged.Clear();
			}
		}

		private static string FindCaller(StackTrace stackTrace)
		{
			foreach (StackFrame frame in stackTrace.GetFrames())
			{
				MethodBase method = frame.GetMethod();
				Type type = method == null ? null : method.DeclaringType;
				if (type == null || IsInternal(type))
				{
					continue;
				}
				string file = frame.GetFileName();
				string location = file == null
					? ""
					: string.Concat(" (", Path.GetFileName(file), ":", frame.GetFileLineNumber().ToString(), ")");
				return string.Concat(OuterType(type).Name, ".", method.Name, location);
			}
			return "(unknown)";
		}

		private static bool IsInternal(Type type)
		{
			Type outer = OuterType(type);
			return outer == typeof(Util) || outer == typeof(SelectorTrace);
		}

		// 람다·반복기가 만든 중첩 타입(Util+<>c 등)을 바깥 타입으로 정리한다.
		private static Type OuterType(Type type)
		{
			while (type.DeclaringType != null)
			{
				type = type.DeclaringType;
			}
			return type;
		}

		private static string SafeUrl(IWebDriver driver)
		{
			if (driver == null)
			{
				return "(no driver)";
			}
			try
			{
				return driver.Url;
			}
			catch
			{
				return "(url unavailable)";
			}
		}
	}
}
```

- [ ] **Step 4: `Util`에서 호출**

`BandProgram.Core/Util.cs`의 네 메서드를 바꾼다. 반환값과 예외 흐름은 그대로다.

```csharp
		public IWebElement findElement(string str)
		{
			IWebElement webElement;
			try
			{
				webElement = this.driver.FindElement(By.CssSelector(str));
			}
			catch
			{
				webElement = null;
				SelectorTrace.Miss(str, this.driver);
			}
			return webElement;
		}

		public IWebElement findElement(IWebElement element, string str)
		{
			IWebElement webElement;
			try
			{
				webElement = element.FindElement(By.CssSelector(str));
			}
			catch
			{
				webElement = null;
				SelectorTrace.Miss(str, this.driver);
			}
			return webElement;
		}

		public IReadOnlyCollection<IWebElement> findElements(string str)
		{
			IReadOnlyCollection<IWebElement> webElements = this.driver.FindElements(By.CssSelector(str));
			if (webElements.Count == 0)
			{
				SelectorTrace.Miss(str, this.driver);
			}
			return webElements;
		}

		public IReadOnlyCollection<IWebElement> findElementsWithXPath(string str)
		{
			IReadOnlyCollection<IWebElement> webElements = this.driver.FindElements(By.XPath(str));
			if (webElements.Count == 0)
			{
				SelectorTrace.Miss(str, this.driver);
			}
			return webElements;
		}
```

- [ ] **Step 5: 테스트 통과 확인**

Run: `dotnet test BandProgram.Tests --filter SelectorTraceTests 2>&1 | tail -3`
Expected: 통과, 실패 0

- [ ] **Step 6: WinForms 싱크 연결**

`BandProgram/Program.cs`의 `Main`에서 `Util.Clipboard = ...` 다음 줄에 추가한다(`using System.IO;` 필요). 고객이 보는 `사용기록.txt`와 분리한다.

```csharp
            SelectorTrace.Sink = line =>
                File.AppendAllText(Path.Combine(AppPaths.DataDir, "selector-miss.log"),
                    string.Concat(DateTime.Now.ToString("yy-MM-dd HH:mm:ss"), " ", line, Environment.NewLine));
```

- [ ] **Step 7: 전체 빌드·테스트**

Run: `dotnet build BandProgram.sln 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개" && dotnet test BandProgram.Tests 2>&1 | tail -3`
Expected: `오류 0개`, 테스트 통과

- [ ] **Step 8: Commit**

```bash
git add -A BandProgram BandProgram.Core BandProgram.Tests
git commit -m "Log selector misses with the calling location and page snapshot

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: `BandProgram.Dev` 대화형 셸

**Files:**
- Create: `BandProgram.Dev/BandProgram.Dev.csproj`, `Program.cs`, `DevShell.cs`, `ShellCommand.cs`, `DevConfig.cs`
- Create: `devdata.example/dev.json`
- Create: `.vscode/launch.json`, `.vscode/tasks.json`
- Create: `docs/dev-on-mac.md`
- Modify: `BandProgram.sln`
- Test: `BandProgram.Tests/ShellCommandTests.cs`, `BandProgram.Tests/DevConfigTests.cs`

**Interfaces:**
- Consumes: `AppPaths.DataDir`, `Util.Clipboard`, `WorkControl`, `SelectorTrace.Sink/SnapshotEnabled/SaveSnapshot`, `FunctionList` 공개·internal 메서드(`login()`, `startChrome(int)`, `closeChrome()`, `getBandList(List<BandInfo>)`, `setBandListFromQuery(string,int,int,int)`, `getBandListFromQuery()`, `getBandListFromQueryWithMemCnt()`, `getBandInfoFromUrl(string)`, `signupBand(BandInfo,string)`, `setPostingParam/setCommentParam/setChattingParam(int,int,bool,bool,int,int,int,int)`, `startPosting/startComment/startChatting(Del_PrintLog, Del_PrintLogLeft)`), `Util.readAll`, `Util.writeStream`, `Util.Base64Encoding/Base64Decoding`, `Util.setBandAccount`, `Util.getDriver()`, `Util.currentUrl()`, `Util.goToUrl(string,int)`
- Produces:
  - `ShellCommand.Parse(string line) : ShellCommand` with `string Name`, `List<string> Args`, `Dictionary<string,string> Options`, `string Rest`(이름 뒤 원문), `int IntOption(string key, int fallback)`
  - `DevConfig.Load(string path) : DevConfig` with `JobParams Posting, Comment, Chatting`
  - `JobParams { int Type; int BetweenWorkSec; bool Reserved; bool Paste; int ReserveHour; int ReserveMin; int RepeatCount; int RepeatBetweenSec; }`

`dev.json`의 값은 `BandLayout.setOption`이 콤보박스에서 읽는 값과 같다: `Type`=첫 콤보 인덱스, `BetweenWorkSec`=작업 간격(초), `ReserveHour/Min`=예약 시각, `RepeatCount`=반복 횟수(0은 무한), `RepeatBetweenSec`=반복 간격(초).

- [ ] **Step 1: 프로젝트 만들기**

```bash
mkdir -p BandProgram.Dev
```

`BandProgram.Dev/BandProgram.Dev.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>BandProgram.Dev</RootNamespace>
    <Nullable>disable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="BandProgram.Tests" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BandProgram.Core\BandProgram.Core.csproj" />
  </ItemGroup>

</Project>
```

```bash
dotnet sln BandProgram.sln add BandProgram.Dev/BandProgram.Dev.csproj
dotnet add BandProgram.Tests reference BandProgram.Dev/BandProgram.Dev.csproj
```

- [ ] **Step 2: 실패하는 테스트 작성**

`BandProgram.Tests/ShellCommandTests.cs`:

```csharp
using BandProgram.Dev;

namespace BandProgram.Tests;

public class ShellCommandTests
{
    [Fact]
    public void Parses_name_args_and_options()
    {
        ShellCommand cmd = ShellCommand.Parse("search 캠핑 --min 10 --max 500");
        Assert.Equal("search", cmd.Name);
        Assert.Equal(new[] { "캠핑" }, cmd.Args);
        Assert.Equal(10, cmd.IntOption("min", -1));
        Assert.Equal(500, cmd.IntOption("max", -1));
        Assert.Equal(-1, cmd.IntOption("cnt", -1));
    }

    [Fact]
    public void Double_quotes_group_words()
    {
        ShellCommand cmd = ShellCommand.Parse("signup https://band.us/band/123 \"내 닉네임\"");
        Assert.Equal(new[] { "https://band.us/band/123", "내 닉네임" }, cmd.Args);
    }

    [Fact]
    public void Rest_keeps_raw_text_for_selectors()
    {
        ShellCommand cmd = ShellCommand.Parse("sel   [class='cCoverList'] li");
        Assert.Equal("sel", cmd.Name);
        Assert.Equal("[class='cCoverList'] li", cmd.Rest);
    }

    [Fact]
    public void Name_is_lowercased_and_blank_line_is_empty()
    {
        Assert.Equal("post", ShellCommand.Parse("POST").Name);
        Assert.Equal("", ShellCommand.Parse("   ").Name);
    }

    [Fact]
    public void Invalid_number_option_uses_fallback()
    {
        Assert.Equal(7, ShellCommand.Parse("search a --min abc").IntOption("min", 7));
    }
}
```

`BandProgram.Tests/DevConfigTests.cs`:

```csharp
using BandProgram.Dev;

namespace BandProgram.Tests;

public class DevConfigTests
{
    [Fact]
    public void Loads_job_params_from_json()
    {
        string file = Path.Combine(Path.GetTempPath(), $"dev-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, """
        {
          "posting":  { "type": 1, "betweenWorkSec": 60, "reserved": false, "paste": true,
                        "reserveHour": 9, "reserveMin": 30, "repeatCount": 0, "repeatBetweenSec": 3600 }
        }
        """);
        try
        {
            DevConfig config = DevConfig.Load(file);
            Assert.Equal(1, config.Posting.Type);
            Assert.Equal(60, config.Posting.BetweenWorkSec);
            Assert.True(config.Posting.Paste);
            Assert.Equal(9, config.Posting.ReserveHour);
            Assert.Equal(3600, config.Posting.RepeatBetweenSec);
            Assert.NotNull(config.Comment); // 빠진 항목은 기본값
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Example_file_in_repo_is_valid()
    {
        string example = Path.Combine(FindRepoRoot(), "devdata.example", "dev.json");
        DevConfig config = DevConfig.Load(example);
        Assert.NotNull(config.Posting);
        Assert.NotNull(config.Comment);
        Assert.NotNull(config.Chatting);
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(dir, "BandProgram.sln")))
        {
            dir = Path.GetDirectoryName(dir) ?? throw new InvalidOperationException("repo root not found");
        }
        return dir;
    }
}
```

Run: `dotnet test BandProgram.Tests --filter "ShellCommandTests|DevConfigTests" 2>&1 | grep -E " error " | head -3`
Expected: `ShellCommand`/`DevConfig`를 찾을 수 없음

- [ ] **Step 3: `ShellCommand` 구현**

`BandProgram.Dev/ShellCommand.cs`:

```csharp
using System.Text;

namespace BandProgram.Dev;

internal sealed class ShellCommand
{
    public string Name { get; private set; } = "";
    public List<string> Args { get; } = new();
    public Dictionary<string, string> Options { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string Rest { get; private set; } = "";

    public static ShellCommand Parse(string line)
    {
        var cmd = new ShellCommand();
        string trimmed = (line ?? "").Trim();
        if (trimmed.Length == 0) return cmd;

        int space = trimmed.IndexOf(' ');
        cmd.Name = (space < 0 ? trimmed : trimmed[..space]).ToLowerInvariant();
        cmd.Rest = space < 0 ? "" : trimmed[(space + 1)..].Trim();

        List<string> tokens = Tokenize(cmd.Rest);
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].StartsWith("--") && i + 1 < tokens.Count)
            {
                cmd.Options[tokens[i][2..]] = tokens[i + 1];
                i++;
            }
            else
            {
                cmd.Args.Add(tokens[i]);
            }
        }
        return cmd;
    }

    public int IntOption(string key, int fallback)
    {
        return Options.TryGetValue(key, out string value) && int.TryParse(value, out int n) ? n : fallback;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        bool quoted = false;
        foreach (char c in text)
        {
            if (c == '"') { quoted = !quoted; continue; }
            if (c == ' ' && !quoted)
            {
                if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                continue;
            }
            current.Append(c);
        }
        if (current.Length > 0) tokens.Add(current.ToString());
        return tokens;
    }
}
```

- [ ] **Step 4: `DevConfig` 구현과 예시 파일**

`BandProgram.Dev/DevConfig.cs`:

```csharp
using System.Text.Json;

namespace BandProgram.Dev;

internal sealed class JobParams
{
    public int Type { get; set; }
    public int BetweenWorkSec { get; set; } = 60;
    public bool Reserved { get; set; }
    public bool Paste { get; set; } = true;
    public int ReserveHour { get; set; }
    public int ReserveMin { get; set; }
    public int RepeatCount { get; set; } = 1;
    public int RepeatBetweenSec { get; set; } = 60;
}

internal sealed class DevConfig
{
    public JobParams Posting { get; set; } = new();
    public JobParams Comment { get; set; } = new();
    public JobParams Chatting { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static DevConfig Load(string path)
    {
        DevConfig config = JsonSerializer.Deserialize<DevConfig>(File.ReadAllText(path), Options) ?? new DevConfig();
        config.Posting ??= new JobParams();
        config.Comment ??= new JobParams();
        config.Chatting ??= new JobParams();
        return config;
    }
}
```

`devdata.example/dev.json`:

```json
{
  // BandLayout.setOption 과 같은 값. type = 첫 번째 콤보박스 인덱스
  "posting":  { "type": 0, "betweenWorkSec": 60, "reserved": false, "paste": true,
                "reserveHour": 0, "reserveMin": 0, "repeatCount": 1, "repeatBetweenSec": 60 },
  "comment":  { "type": 0, "betweenWorkSec": 60, "reserved": false, "paste": true,
                "reserveHour": 0, "reserveMin": 0, "repeatCount": 1, "repeatBetweenSec": 60 },
  "chatting": { "type": 0, "betweenWorkSec": 60, "reserved": false, "paste": true,
                "reserveHour": 0, "reserveMin": 0, "repeatCount": 1, "repeatBetweenSec": 60 }
}
```

Run: `dotnet test BandProgram.Tests --filter "ShellCommandTests|DevConfigTests" 2>&1 | tail -3`
Expected: 통과, 실패 0

- [ ] **Step 5: `DevShell` 구현**

`BandProgram.Dev/DevShell.cs`:

```csharp
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
                case "sel": Select(cmd.Rest, By.CssSelector(cmd.Rest)); return true;
                case "xpath": Select(cmd.Rest, By.XPath(cmd.Rest)); return true;
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

    // bandAccount.txt는 CP949이고 비밀번호는 Base64다(LoginSecond.buttonAdd_Click과 같은 형식).
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
        if (text.Length == 0) { Log("sel <css> / xpath <xpath>", ConsoleColor.Red); return; }
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
```

- [ ] **Step 6: `Program` 구현**

`BandProgram.Dev/Program.cs`:

```csharp
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
```

- [ ] **Step 7: 빌드와 기본 동작 확인 (브라우저 없이)**

Run: `dotnet build BandProgram.sln 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개"`
Expected: `오류 0개`

Run:
```bash
D=$(mktemp -d) && printf 'help\naccount add 01012345678 pw1234 전화번호\naccount\nsel .x\nfoo\nquit\n' | dotnet run --project BandProgram.Dev -- --data "$D" ; iconv -f cp949 -t utf-8 "$D/bandAccount.txt"
```
Expected: 도움말 출력, `추가됨: 01012345678 (전화번호)`, 계정 목록에 `01012345678 (전화번호)`, `sel`은 `브라우저가 없어요`, `foo`는 `알 수 없는 명령`, 마지막 줄 `01012345678	cHcxMjM0	전화번호`

- [ ] **Step 8: VS Code 구성과 안내 문서**

`.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build-dev",
      "type": "process",
      "command": "dotnet",
      "args": ["build", "${workspaceFolder}/BandProgram.Dev/BandProgram.Dev.csproj"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

`.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Dev 셸",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build-dev",
      "program": "${workspaceFolder}/BandProgram.Dev/bin/Debug/net10.0/BandProgram.Dev.dll",
      "args": ["--data", "${workspaceFolder}/devdata"],
      "cwd": "${workspaceFolder}",
      "console": "integratedTerminal",
      "stopAtEntry": false
    }
  ]
}
```

`docs/dev-on-mac.md`:

````markdown
# 맥에서 개발·디버깅하기

## 준비

1. .NET SDK 10, Google Chrome, VS Code + C# Dev Kit 확장
2. 데이터 폴더 만들기:
   ```bash
   mkdir -p devdata && cp devdata.example/dev.json devdata/
   ```
   `devdata/`는 git에 올라가지 않는다. 계정·쿠키·스냅샷이 여기에 쌓인다.
3. 계정 추가(CP949로 저장되므로 텍스트 편집기 대신 셸 명령을 쓴다):
   ```
   band> account add <아이디> <비밀번호> 전화번호
   ```
4. 포스팅·댓글·채팅 원고는 `devdata/AutoDoc/Posting/post_1/contents.txt` 같은 구조로 둔다(CP949).
   Windows에서 쓰던 `AutoDoc` 폴더를 그대로 복사해도 된다. 대상 밴드 목록은 `devdata/bandList.txt`.

## 실행

- 터미널: `dotnet run --project BandProgram.Dev -- --data devdata`
- VS Code: 실행 및 디버그 → "Dev 셸" → F5. 브레이크포인트는 `BandProgram.Core`의 어느 줄에나 걸 수 있다.

`help`로 명령 목록을 본다. 로그인한 브라우저는 `quit` 전까지 유지된다.

**주의:** `post`, `comment`, `chat`, `signup`은 실제로 글을 쓰거나 가입한다. 테스트용 밴드에서 실행한다.

## 셀렉터가 깨졌을 때

1. 문제가 된 작업을 실행한다(예: `search 캠핑`).
2. 노란색 `[SELECTOR MISS]` 줄에서 셀렉터와 위치(`FunctionList.cs:416`)를 확인한다.
   `snapshot:` 폴더에 그 순간의 `page.html`, `screen.png`가 있다.
3. 열려 있는 Chrome에서 우클릭 → 검사로 새 구조를 찾는다.
4. `sel <새 셀렉터>`로 개수와 텍스트를 확인한다.
5. 해당 줄을 고치고 다시 실행한다.

`[SELECTOR MISS]`는 "있으면 처리, 없으면 넘어감" 같은 정상적인 존재 확인에서도 찍힌다.
작업 결과가 이상할 때 그 직전의 기록을 본다.

## 예외에서 바로 멈추기

VS Code의 실행 및 디버그 → 중단점 패널에서 "All Exceptions"(또는 "User-Unhandled Exceptions")를 켜면
`catch {}`에 삼켜지기 전의 예외에서 멈출 수 있다. 정상 흐름에서도 예외가 자주 나므로 필요할 때만 켠다.

## 맥에서 다른 점

- 붙여넣기는 `Cmd+V`로 한다. 헤드리스 Chrome에서는 붙여넣기가 되지 않는다.
- `adb`는 PATH에서 찾는다(`brew install android-platform-tools`). IP 변경 기능은 현재 호출되지 않는다.
- WinForms 화면은 맥에서 띄울 수 없다. 화면 변경은 Windows에서 확인한다(`docs/windows-smoke-test.md`).
````

- [ ] **Step 9: 전체 테스트**

Run: `dotnet test BandProgram.Tests 2>&1 | tail -3`
Expected: 통과, 실패 0

- [ ] **Step 10: Commit**

```bash
git add -A BandProgram.Dev BandProgram.Tests BandProgram.sln devdata.example .vscode docs/dev-on-mac.md
git commit -m "Add interactive Dev shell for running automation on macOS

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Selenium 4 업그레이드

Selenium 4.50.0은 기존 코드 수정 없이 컴파일되고(시험 빌드로 확인), Selenium Manager가 설치된 Chrome에 맞는 드라이버를 받는다.

**Files:**
- Modify: `BandProgram.Core/BandProgram.Core.csproj`
- Modify: `BandProgram/BandProgram.csproj` (`chromedriver.exe` 항목 제거)
- Delete: `BandProgram/chromedriver.exe`

**Interfaces:** 변경 없음

- [ ] **Step 1: 지금은 맥에서 Chrome이 뜨지 않는 것 확인**

Run: `printf 'chrome\nquit\n' | dotnet run --project BandProgram.Dev -- --data "$(mktemp -d)" 2>&1 | grep -E "Chrome"`
Expected: `Chrome 실행 실패` (Selenium 3.141은 맥용 chromedriver를 찾지 못함)

- [ ] **Step 2: 패키지 버전 올리기**

`BandProgram.Core/BandProgram.Core.csproj`:

```xml
    <PackageReference Include="Selenium.WebDriver" Version="4.50.0" />
    <PackageReference Include="Selenium.Support" Version="4.50.0" />
```

`BandProgram/BandProgram.csproj`에서 `chromedriver.exe` `Content` 항목이 있는 `ItemGroup`을 지우고 파일도 지운다.

```bash
git rm -q BandProgram/chromedriver.exe
```

- [ ] **Step 3: 빌드와 테스트**

Run: `dotnet build BandProgram.sln --no-incremental 2>&1 | grep -E "빌드했습니다|오류 [0-9]+개" && dotnet test BandProgram.Tests 2>&1 | tail -3`
Expected: `오류 0개`, 테스트 통과

- [ ] **Step 4: 맥에서 Chrome 실행·셀렉터·종료 확인**

Run:
```bash
D=$(mktemp -d) && printf 'chrome\nurl data:text/html,<ul class=list><li>a</li><li>b</li></ul>\nsel .list li\nsnap\nquit\n' | dotnet run --project BandProgram.Dev -- --data "$D" 2>&1 | tail -8; ls "$D"; pgrep -x chromedriver || echo "chromedriver 종료됨"
```
Expected: `Chrome 실행됨`, `sel` 결과 `2개`와 `[0] <li ...> a`, `스냅샷: .../failures/...`, 데이터 폴더에 `chromedata`, `failures`가 생김, 마지막 줄 `chromedriver 종료됨`

- [ ] **Step 5: Commit**

```bash
git add -A BandProgram BandProgram.Core
git commit -m "Upgrade Selenium to 4.50 and drop the bundled chromedriver

Selenium Manager now downloads the driver matching the installed Chrome.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Windows 게시 스크립트와 스모크 테스트 체크리스트

**Files:**
- Create: `scripts/publish-windows.sh`
- Create: `docs/windows-smoke-test.md`

**Interfaces:** 없음

- [ ] **Step 1: 게시 스크립트**

`scripts/publish-windows.sh`:

```bash
#!/usr/bin/env bash
# 맥에서 고객용 Windows 실행 파일을 만든다. 결과: publish/win-x64/BandProgram.exe
set -euo pipefail
cd "$(dirname "$0")/.."
rm -rf publish/win-x64
dotnet publish BandProgram/BandProgram.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
echo "완료: publish/win-x64/BandProgram.exe"
```

```bash
chmod +x scripts/publish-windows.sh
```

- [ ] **Step 2: 게시 확인**

Run: `scripts/publish-windows.sh 2>&1 | tail -1 && ls publish/win-x64 | grep -E "^BandProgram(\.Core)?\.(exe|dll)$|selenium-manager"`
Expected: `완료: ...`, `BandProgram.exe`, `BandProgram.dll`, `BandProgram.Core.dll`, 그리고 `selenium-manager` 폴더(또는 `selenium-manager.exe`)

`selenium-manager`가 없으면 Selenium이 드라이버를 받지 못한다. 이 경우 `ls -R publish/win-x64 | grep -i selenium-manager`로 위치를 확인하고, 없으면 보고한다.

- [ ] **Step 3: 스모크 테스트 체크리스트**

`docs/windows-smoke-test.md`:

```markdown
# Windows 스모크 테스트

맥에서 `scripts/publish-windows.sh`로 만든 `publish/win-x64` 폴더를 Windows PC로 복사해서 확인한다.
기존 고객 데이터(`bandList.txt`, `bandAccount.txt`, `AutoDoc/`)를 같은 폴더에 함께 복사한다.
.NET 런타임은 포함되어 있어 따로 설치하지 않는다.

| # | 확인 | 기대 결과 | 결과 |
|---|---|---|---|
| 1 | `BandProgram.exe` 실행 | 로그인 창이 기존과 같은 글꼴·배치로 뜬다 | |
| 2 | 라이선스 로그인 | 계정 선택 화면으로 넘어간다 | |
| 3 | 계정 목록 | 기존 `bandAccount.txt`의 계정이 한글 유형(전화번호/이메일)과 함께 보인다 | |
| 4 | 계정 목록 우클릭 → 선택된 항목 삭제 | 메뉴가 뜨고 삭제된다 | |
| 5 | 계정 로그인 | Chrome이 열리고 로그인된다(chromedriver 자동 다운로드, 첫 실행은 느릴 수 있음) | |
| 6 | 밴드 목록 불러오기 | 목록이 뜨고 한글 이름이 깨지지 않는다 | |
| 7 | 밴드 목록 우클릭 메뉴 10개 항목 | 모든 항목이 기존처럼 동작한다(구분선 포함 위치) | |
| 8 | 포스팅 원고 추가 → 이미지 추가 → 저장 | 파일 선택 창이 뜨고, 저장 후 목록에 보인다 | |
| 9 | 원고 이미지 더블클릭 | 이미지 또는 폴더가 열린다 | |
| 10 | 포스팅 시작 | 테스트 밴드에 1건 작성된다(붙여넣기 포함) | |
| 11 | 포스팅 일시정지 → 1~2초 안에 멈춤 → 재개 | 멈췄다가 이어서 진행된다 | |
| 12 | 포스팅 일시정지 상태에서 댓글 시작 → 댓글 진행 중 포스팅 재개 | 둘 다 진행된다 | |
| 13 | 포스팅 초기화 | 작업이 멈추고 버튼이 대기 상태로 돌아간다. 다시 시작하면 처음부터 진행된다 | |
| 14 | 가입 시작 → 일시정지 → 재개 → 초기화 | 각 단계가 기존처럼 동작한다 | |
| 15 | 새로 저장한 원고를 메모장으로 열기 | 한글이 깨지지 않는다(CP949) | |
| 16 | `selector-miss.log` | 실행 폴더에 생기고, 셀렉터 실패가 기록된다 | |
| 17 | 계정 변경 버튼 / 창 닫기 | Chrome이 닫히고 프로그램이 정상 종료된다 | |
```

- [ ] **Step 4: Commit**

```bash
git add scripts/publish-windows.sh docs/windows-smoke-test.md
git commit -m "Add Windows publish script and smoke test checklist

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
