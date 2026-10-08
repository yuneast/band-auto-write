#!/usr/bin/env bash
# 맥에서 고객용 Windows 실행 파일을 만든다.
# 결과: publish/win-x64/BandProgram.exe (런타임 포함 단일 파일) + selenium-manager.exe
# selenium-manager.exe는 Selenium이 exe 옆에서 찾으므로 함께 배포해야 한다.
set -euo pipefail
cd "$(dirname "$0")/.."
rm -rf publish/win-x64
dotnet publish BandProgram/BandProgram.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=embedded \
  -o publish/win-x64
echo "완료: publish/win-x64/ (BandProgram.exe, selenium-manager.exe)"
