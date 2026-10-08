#!/usr/bin/env bash
# 맥에서 고객용 Windows 실행 파일을 만들고 서버에 올린다.
#
# 결과:
#   publish/win-x64/BandProgram.exe     런타임 포함 단일 파일
#   publish/win-x64/selenium-manager.exe  Selenium이 exe 옆에서 찾으므로 함께 배포
#   publish/BandProgram.zip             위 두 파일을 묶은 고객 배포용 zip
#
# 업로드: http://newsoft.kr/download/BandProgram.zip (서버 /www/band/download/)
#   날짜가 붙은 사본을 서버 /root/band-releases/ 에 남긴다(웹 비공개, 되돌리기용).
#   SSH 키 인증을 쓴다. 서버 주소는 BAND_DEPLOY_HOST / BAND_DEPLOY_PORT로 바꿀 수 있다.
#
# 사용법: scripts/publish-windows.sh [--no-upload]
set -euo pipefail
cd "$(dirname "$0")/.."

UPLOAD=1
for arg in "$@"; do
  case "$arg" in
    --no-upload) UPLOAD=0 ;;
    *) echo "알 수 없는 옵션: $arg (사용법: $0 [--no-upload])" >&2; exit 2 ;;
  esac
done

DEPLOY_HOST="${BAND_DEPLOY_HOST:-192.168.1.1}"
DEPLOY_PORT="${BAND_DEPLOY_PORT:-47839}"
DEPLOY_USER="${BAND_DEPLOY_USER:-root}"
WEB_DIR=/www/band/download
RELEASE_DIR=/root/band-releases
DOWNLOAD_URL=http://newsoft.kr/download/BandProgram.zip

rm -rf publish/win-x64 publish/BandProgram.zip
dotnet publish BandProgram/BandProgram.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=embedded \
  -o publish/win-x64
(cd publish/win-x64 && zip -q -X ../BandProgram.zip BandProgram.exe selenium-manager.exe)
echo "빌드 완료: publish/BandProgram.zip"

if [ "$UPLOAD" -eq 0 ]; then
  exit 0
fi

SSH=(ssh -p "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 "$DEPLOY_USER@$DEPLOY_HOST")
STAMP=$(date +%Y%m%d-%H%M%S)
LOCAL_SHA=$(shasum -a 256 publish/BandProgram.zip | cut -d' ' -f1)

echo "업로드 중: $DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PORT"
"${SSH[@]}" "mkdir -p $WEB_DIR $RELEASE_DIR"
scp -q -P "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 \
  publish/BandProgram.zip "$DEPLOY_USER@$DEPLOY_HOST:$WEB_DIR/.BandProgram.zip.uploading"

# 받는 도중의 고객이 반쯤 올라간 파일을 받지 않도록 이름 바꾸기로 교체한다.
REMOTE_SHA=$("${SSH[@]}" "set -e
  cd $WEB_DIR
  cp .BandProgram.zip.uploading $RELEASE_DIR/BandProgram-$STAMP.zip
  chmod 644 .BandProgram.zip.uploading
  mv -f .BandProgram.zip.uploading BandProgram.zip
  sha256sum BandProgram.zip | cut -d' ' -f1")

if [ "$LOCAL_SHA" != "$REMOTE_SHA" ]; then
  echo "업로드 확인 실패: 해시가 다릅니다 (로컬 $LOCAL_SHA, 서버 $REMOTE_SHA)" >&2
  exit 1
fi
echo "업로드 완료: $DOWNLOAD_URL"
echo "보관본: $RELEASE_DIR/BandProgram-$STAMP.zip (sha256 $LOCAL_SHA)"
