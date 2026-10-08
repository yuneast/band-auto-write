# 맥에서 개발·디버깅하기

## 준비

1. .NET SDK 10, Google Chrome, VS Code + C# Dev Kit 확장
2. 데이터 폴더 만들기:
   ```bash
   mkdir -p devdata && cp devdata.example/dev.json devdata/
   ```
   `devdata/`는 git에 올라가지 않는다. 계정·쿠키·스냅샷이 여기에 쌓인다.
3. 계정 추가. `bandAccount.txt`의 비밀번호는 Base64라서 셸 명령이 편하다:
   ```
   band> account add <아이디> <비밀번호> 전화번호
   ```
4. 포스팅·댓글·채팅 원고는 `devdata/AutoDoc/Posting/post_1/contents.txt` 같은 구조로 둔다.
   모든 텍스트 파일은 UTF-8이라 VS Code에서 바로 편집하면 된다. 대상 밴드 목록은 `devdata/bandList.txt`.
5. Windows에서 쓰던 `AutoDoc` 폴더나 `bandList.txt`를 복사해 와도 된다. CP949 파일은 Dev 셸을 켤 때
   UTF-8로 바뀌고 원본은 `devdata/backup-cp949/`에 남는다.

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

## 배포와 자동 업데이트

- 처음 한 번: `scripts/create-update-key.sh`로 서명 키를 만든다(이미 있으면 멈춘다).
  개인키 `~/.config/band-deploy/update-signing-key.pem`은 저장소에 넣지 말고 따로 백업한다.
  잃어버리면 새 키를 만들고 공개키를 `BandProgram.Core/Update/UpdatePublicKey.cs`에 넣은 exe를 고객에게 직접 한 번 배포해야 한다.
- 배포: `scripts/publish-windows.sh` — 버전(실행 시각) 지정, 빌드, zip, `version.json` 서명, 업로드, 서명 재검증까지 한다.
  빌드만 하려면 `--no-upload`.
- 고객 프로그램은 시작할 때 `http://newsoft.kr/download/version.json`을 확인해 새 버전이면 묻지 않고 업데이트한다.
- 문제 있는 배포는 옛 코드로 다시 배포해서 되돌린다(더 높은 버전 번호로 올라간다). `version.json`을 옛 버전으로 바꿔도 고객은 내려가지 않는다.
