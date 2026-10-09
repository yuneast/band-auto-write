# BandProgram 자동 업데이트 설계

- 작성일: 2026-10-08
- 상태: 검토 대기

## 1. 목적과 범위

### 목적
고객이 프로그램을 실행하면 서버의 최신 버전을 확인하고, 새 버전이 있으면 묻지 않고 업데이트한 뒤
다시 실행한다. 셀렉터 수정 같은 변경이 고객에게 바로 반영되게 하는 것이 목적이다.

### 결정 사항 (사용자 확인)
- 업데이트 정책: **강제 업데이트** (새 버전이 있으면 바로 업데이트하고 재시작)
- 무결성: **서명 검증** (`newsoft.kr`은 HTTP만 제공하므로 바꿔치기한 업데이트를 서명으로 막는다)
- 구조: **앱 안에 업데이트 기능 내장**. 별도 업데이터 exe나 설치형 라이브러리는 쓰지 않는다.

### 제약
- 배포 파일은 지금처럼 `BandProgram.exe`(런타임 포함 단일 파일)와 `selenium-manager.exe` 두 개다.
  고객은 데이터(`bandList.txt`, `bandAccount.txt`, `AutoDoc/` 등)와 같은 폴더에 둔다.
- 서버: `newsoft.kr` = OpenWrt 장비의 nginx, 웹 루트 `/www/band`, HTTP 80만 열려 있다.
  배포는 `scripts/publish-windows.sh`가 SSH 키로 올린다(현재 구현됨).
- 서명 알고리즘은 .NET 10에 내장되고 맥 `openssl`로도 서명할 수 있는 **ECDSA P-256 + SHA-256**을 쓴다.
  (.NET에는 Ed25519가 기본으로 들어 있지 않다.)

### 성공 기준
1. 업데이트 기능이 들어간 버전 A를 실행한 상태에서 맥이 버전 B를 배포하면, A를 다시 실행했을 때 B로 바뀌어 재시작된다.
2. 서명이 틀리거나, 해시가 틀리거나, 서버 버전이 같거나 낮으면 업데이트하지 않는다.
3. 오프라인이거나 서버가 응답하지 않아도 5초 남짓 안에 지금 버전으로 실행된다.
4. 업데이트가 실패해도 프로그램은 지금 버전으로 실행된다(파일이 반쯤 바뀐 상태로 남지 않는다).

### 범위 밖
- HTTPS 적용, 코드 서명 인증서(Authenticode)
- 업데이트 거부·연기 옵션, 변경 내역 표시
- Dev 셸 업데이트
- 업데이트 기능이 없는 기존 exe의 자동 전환(첫 버전은 지금처럼 zip으로 직접 배포한다)

## 2. 버전과 배포 (서버 쪽)

### 버전 번호
`publish-windows.sh`가 실행 시각으로 버전을 정한다: `yyyy.M.d.HHmm` (예: `2026.10.8.1015`).
`dotnet publish`에 `-p:Version=<버전>`으로 넘겨 어셈블리 버전과 파일 버전에 넣는다.
네 자리 모두 65534 이하라 어셈블리 버전 제약에 맞는다. 같은 분에 두 번 배포하면 같은 버전이 되므로,
스크립트는 서버의 현재 버전과 같거나 낮으면 업로드를 멈춘다.

### 서버 파일 (`/www/band/download/`)
```
BandProgram-<버전>.zip   버전별 zip (BandProgram.exe + selenium-manager.exe)
BandProgram.zip          최신본 사본 (신규 고객 수동 다운로드용)
version.json             최신 버전 정보
version.json.sig         version.json 바이트 전체에 대한 ECDSA P-256/SHA-256 서명 (DER, 바이너리)
```
`version.json` (UTF-8, BOM 없음):
```json
{ "version": "2026.10.8.1015",
  "url": "http://newsoft.kr/download/BandProgram-2026.10.8.1015.zip",
  "sha256": "<zip의 SHA-256 소문자 16진수>" }
```
`/root/band-releases/`에 날짜 붙은 보관본을 남기는 현재 동작은 유지한다.

### 업로드 순서
1. 버전별 zip과 `BandProgram.zip`을 올린다(임시 이름 → 이름 바꾸기).
2. `version.json`과 `version.json.sig`를 임시 이름으로 올린 뒤 **서명 파일을 먼저, 그다음 `version.json`** 순으로 이름을 바꾼다.
   두 파일이 잠깐 어긋나는 순간에 읽은 고객은 서명 검증에 실패해 지금 버전으로 실행되고, 다음 실행 때 업데이트된다.
3. 업로드 후 서버의 `version.json`과 서명을 다시 내려받아 맥에서 `openssl dgst -verify`로 검증한다. 실패하면 오류로 끝낸다.

버전별 zip을 따로 두므로, 업로드 도중 옛 `version.json`을 읽은 고객도 옛 zip을 정상적으로 받는다.

### 서명 키
- `scripts/create-update-key.sh`(새 파일)로 한 번 만든다.
  - 개인키: `~/.config/band-deploy/update-signing-key.pem` (권한 600, 저장소에 넣지 않음)
  - 공개키: SubjectPublicKeyInfo(DER)를 Base64로 출력해서 `BandProgram.Core/Update/UpdatePublicKey.cs` 상수에 넣는다.
  - 이미 개인키가 있으면 덮어쓰지 않고 멈춘다.
- `publish-windows.sh`는 개인키가 없으면 업로드를 멈추고 안내한다. 키를 자동으로 만들지 않는다(앱의 공개키와 어긋나기 때문).
- 개인키를 잃어버리면 새 키를 만들고, 새 공개키를 넣은 exe를 고객에게 한 번 직접 배포해야 한다. 개인키 백업을 안내한다.

### 되돌리기
앱은 서버 버전이 지금보다 **높을 때만** 업데이트하므로 `version.json`을 옛 버전으로 돌려도 고객은 내려가지 않는다.
문제가 있는 배포는 옛 코드로 다시 빌드해 더 높은 버전 번호로 배포해서 되돌린다. 같은 규칙 덕분에 옛 서명 파일을 재전송해 버전을 낮추는 공격도 막힌다.

## 3. 앱 쪽 동작

### 시점
`Program.Main(string[] args)`에서 글꼴·DPI 설정, `Directory.SetCurrentDirectory(AppPaths.DataDir)` 직후,
클립보드·SelectorTrace 설정과 UTF-8 변환, 로그인 창보다 **먼저** 실행한다. 업데이트하면 재시작하므로 그 전에 다른 일을 하지 않는다.
(`Main`은 지금 인자를 받지 않으므로 `string[] args`를 추가한다.)

### 흐름
1. **정리:** exe 폴더의 `BandProgram.old.exe`, `selenium-manager.old.exe`, `.update/`를 지운다. 이전 프로세스가 종료 중일 수 있어
   잠긴 파일은 200ms 간격으로 최대 10번 재시도하고, 그래도 안 되면 넘어간다(다음 실행 때 다시 시도).
2. **건너뛰기:** 인자에 `--updated`가 있으면 확인을 건너뛴다(업데이트 직후 실행). 버전 비교가 잘못되어도 무한 재시작을 막는다.
3. **확인:** `http://newsoft.kr/download/version.json`과 `version.json.sig`를 받는다. 각 요청 시간 제한 5초.
4. **서명 검증:** 앱의 공개키로 `version.json` 바이트에 대한 서명을 검증한다(`ECDsa.VerifyData`, SHA-256, `DSASignatureFormat.Rfc3279DerSequence`).
5. **해석과 버전 비교:** `version`, `url`, `sha256`을 읽는다. 필드가 없거나 `url`이 `http://` 또는 `https://`가 아니면 중단.
   `Version.Parse`로 비교해 서버 버전이 지금 exe 버전(`Assembly.GetEntryAssembly().GetName().Version`)보다 높을 때만 진행한다.
6. **다운로드:** 작은 진행률 창("새 버전으로 업데이트 중… <버전>")을 띄우고 zip을 exe 폴더의 `.update/BandProgram.zip`에 받는다.
   같은 드라이브여야 이름 바꾸기로 교체할 수 있다. 시간 제한 5분.
7. **해시 검증:** 받은 zip의 SHA-256을 서명된 `version.json`의 `sha256`과 비교한다.
8. **압축 풀기:** zip 항목이 정확히 `BandProgram.exe`, `selenium-manager.exe` 두 개(폴더 없이, 대소문자 무시)일 때만
   `.update/new/`에 푼다. 다른 항목, 경로 구분자, `..`이 있으면 거부한다.
9. **교체** (exe 경로는 `Environment.ProcessPath`):
   1. `BandProgram.exe` → `BandProgram.old.exe` (실행 중인 파일도 이름 바꾸기는 허용된다)
   2. `.update/new/BandProgram.exe` → `BandProgram.exe`
   3. `selenium-manager.exe` → `selenium-manager.old.exe`(있을 때), `.update/new/selenium-manager.exe` → `selenium-manager.exe`
   - 어느 단계든 실패하면 지금까지 한 이름 바꾸기를 역순으로 되돌리고 중단한다.
10. **재시작:** 새 `BandProgram.exe`를 원래 인자 + `--updated`로 실행하고 지금 프로세스는 `Environment.Exit(0)`.
    새 프로세스 실행에 실패하면 교체를 되돌리고 지금 버전으로 계속한다.

### 실패 처리
- 어떤 실패(네트워크, 시간 초과, 서명, 해석, 버전, 해시, zip, 교체, 재시작)든 **지금 버전으로 그대로 실행**한다.
- 실패 이유는 exe 폴더의 `update.log`에 시각과 함께 한 줄씩 남긴다. 로그 쓰기 실패는 무시한다.
- 교체 단계에서 쓰기 권한이 없어 실패한 경우(`UnauthorizedAccessException`)에만 메시지 상자로 알린다:
  "업데이트하지 못했습니다. 프로그램 폴더의 쓰기 권한을 확인해 주세요." 고객이 조치해야 해결되기 때문이다.

### 코드 구조
**BandProgram.Core/Update/** (맥에서 테스트 가능, UI 의존 없음)
- `UpdatePublicKey` — 공개키 상수(Base64 SPKI)
- `UpdateManifest` — `version.json` 해석·검증 (`TryParse(byte[], out UpdateManifest, out string error)`)
- `ManifestVerifier` — 서명 검증 (`bool Verify(byte[] manifest, byte[] signature, string publicKeyBase64)`)
- `UpdatePackage` — zip 검사와 풀기, SHA-256 계산
- `UpdateInstaller` — 정리(`Cleanup`), 교체와 되돌리기(`Swap`). 폴더 경로를 받아 동작하므로 임시 폴더로 테스트한다.
- `Updater` — 위를 묶은 흐름. `HttpClient`, 현재 버전, 서버 주소, 공개키, 진행률 콜백을 받아 결과를 돌려준다:
  `NoUpdate` / `Failed(reason, isPermissionError)` / `ReadyToRestart(newExePath)`. 프로세스 실행과 종료는 하지 않는다.

**BandProgram/** (WinForms)
- `UpdateForm` — 진행률 창
- `Program.Main` — `Updater` 실행, `ReadyToRestart`면 새 프로세스 실행 후 종료(실패 시 `UpdateInstaller`로 되돌리기),
  권한 오류 메시지, 그 외에는 기존 시작 흐름으로 계속

## 4. 테스트와 검증

### 단위 테스트 (BandProgram.Tests, 맥)
- `UpdateManifest`: 정상, 깨진 JSON, 필드 누락, `url` 스킴 오류, `sha256` 형식 오류
- `ManifestVerifier`: 테스트 전용 키로 서명한 정상 파일 통과, 내용 한 글자 변경·다른 키·빈/깨진 서명 거부.
  `openssl dgst -sha256 -sign`으로 미리 만든 테스트 전용 키·서명 픽스처를 .NET이 검증하는지(형식 호환) 확인.
- 버전 비교: 높음 진행, 같음·낮음·형식 오류 중단
- `UpdatePackage`: 정상 zip 통과, 추가 항목·경로 포함·`..`·exe 누락 거부, 해시 불일치 거부
- `UpdateInstaller`: 임시 폴더에서 교체 성공, 중간 단계 실패 시 원상 복구, `.old`·`.update` 정리, 잠긴 파일 재시도 후 넘어감
- `--updated` 인자 시 확인 건너뜀

### 통합 테스트 (맥, 실제 HTTP)
- 테스트 안에서 `HttpListener`로 `localhost`에 `version.json`, 서명, zip을 내려주고 `Updater`를 임시 폴더 대상으로 끝까지 실행:
  `ReadyToRestart`와 실제 파일 교체 확인
- 응답 없는 서버(시간 초과) → `Failed`, 해시가 틀린 zip → `Failed`이고 파일은 원래대로

### 배포 스크립트 검증 (실제 서버)
- `publish-windows.sh`가 업로드 후 서버의 `version.json`·서명을 다시 받아 `openssl`로 검증
- 테스트 하나는 저장소 공개키 상수와 맥 개인키가 짝인지 확인(개인키가 없으면 건너뜀)

### Windows 수동 확인 (`docs/windows-smoke-test.md`에 추가)
1. 업데이트 기능이 들어간 버전 A를 설치·실행
2. 맥에서 버전 B 배포 후 A 실행 → 진행률 창, B로 재시작
3. 다음 실행에서 `BandProgram.old.exe` 삭제 확인
4. 인터넷을 끄고 실행 → 5초 남짓 안에 그대로 실행
5. 쓰기 권한 없는 폴더에서 실행 → 권한 안내 후 그대로 실행

## 5. 위험

- **개인키 분실:** 자동 업데이트가 끊긴다. 새 공개키가 든 exe를 한 번 직접 배포해야 한다. 백업 안내로 완화한다.
- **개인키 유출:** 공격자가 서명된 악성 업데이트를 만들 수 있다. 개인키는 맥에만 두고 권한 600으로 보관한다.
- **백신 오탐:** 실행 중인 exe 이름 바꾸기와 자기 재시작은 일부 백신이 의심할 수 있다. 스모크 테스트에서 확인한다.
- **서버 장애:** 업데이트만 안 될 뿐 프로그램 실행은 막지 않는다.
- **Windows에서 직접 실행 확인 불가:** 교체·재시작은 맥에서 임시 폴더로만 검증되므로 Windows 수동 확인에 의존한다.
