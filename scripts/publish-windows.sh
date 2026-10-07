#!/usr/bin/env bash
# 맥에서 고객용 Windows 실행 파일을 만든다. 결과: publish/win-x64/BandProgram.exe
set -euo pipefail
cd "$(dirname "$0")/.."
rm -rf publish/win-x64
dotnet publish BandProgram/BandProgram.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
echo "완료: publish/win-x64/BandProgram.exe"
