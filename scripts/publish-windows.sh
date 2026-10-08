#!/usr/bin/env bash
# 맥에서 고객용 Windows 실행 파일을 만들고, 서명한 업데이트 정보와 함께 서버에 올린다.
#
# 결과 (publish/):
#   win-x64/BandProgram.exe, win-x64/selenium-manager.exe
#   BandProgram-<버전>.zip, BandProgram.zip   고객 배포용 zip (위 두 파일)
#   version.json, version.json.sig           자동 업데이트 정보와 서명
#
# 버전: 실행 시각 yyyy.M.d.HHmm. 서버의 현재 버전보다 높아야 올라간다.
# 업로드: /www/band/download/ (http://newsoft.kr/download/), 보관본 /root/band-releases/
# 서명 키: ~/.config/band-deploy/update-signing-key.pem (scripts/create-update-key.sh로 한 번 만든다)
# 환경 변수: BAND_DEPLOY_HOST, BAND_DEPLOY_PORT, BAND_DEPLOY_USER, BAND_SIGNING_KEY
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
SIGNING_KEY="${BAND_SIGNING_KEY:-$HOME/.config/band-deploy/update-signing-key.pem}"
WEB_DIR=/www/band/download
RELEASE_DIR=/root/band-releases
BASE_URL=http://newsoft.kr/download

SSH=(ssh -p "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 "$DEPLOY_USER@$DEPLOY_HOST")

# $1이 $2보다 높은 버전이면 참
version_gt() {
  [ "$1" != "$2" ] && [ "$(printf '%s\n%s\n' "$1" "$2" | sort -t. -k1,1n -k2,2n -k3,3n -k4,4n | tail -1)" = "$1" ]
}

if [ "$UPLOAD" -eq 1 ] && [ ! -f "$SIGNING_KEY" ]; then
  echo "서명 키가 없습니다: $SIGNING_KEY" >&2
  echo "처음이면 scripts/create-update-key.sh로 만들고 공개키를 UpdatePublicKey.cs에 넣으세요." >&2
  echo "키를 잃어버렸다면 새 키를 만든 뒤, 새 공개키가 든 exe를 고객에게 직접 한 번 배포해야 합니다." >&2
  exit 1
fi

VERSION=$(date +%Y.%m.%d.%H%M | awk -F. '{ printf "%d.%d.%d.%d", $1, $2, $3, $4 }')
ZIP_NAME="BandProgram-$VERSION.zip"

if [ "$UPLOAD" -eq 1 ]; then
  REMOTE_VERSION=$("${SSH[@]}" "cat $WEB_DIR/version.json 2>/dev/null || true" | sed -n 's/.*"version" *: *"\([0-9.]*\)".*/\1/p')
  if [ -n "$REMOTE_VERSION" ] && ! version_gt "$VERSION" "$REMOTE_VERSION"; then
    echo "서버 버전($REMOTE_VERSION)보다 높지 않아 배포하지 않습니다: $VERSION (1분 뒤 다시 실행하세요)" >&2
    exit 1
  fi
fi

rm -rf publish/win-x64 publish/*.zip publish/version.json publish/version.json.sig
dotnet publish BandProgram/BandProgram.csproj -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=embedded \
  -p:Version="$VERSION" \
  -o publish/win-x64
(cd publish/win-x64 && zip -q -X "../$ZIP_NAME" BandProgram.exe selenium-manager.exe)
cp "publish/$ZIP_NAME" publish/BandProgram.zip
SHA=$(shasum -a 256 "publish/$ZIP_NAME" | cut -d' ' -f1)
printf '{\n  "version": "%s",\n  "url": "%s/%s",\n  "sha256": "%s"\n}\n' "$VERSION" "$BASE_URL" "$ZIP_NAME" "$SHA" > publish/version.json
if [ -f "$SIGNING_KEY" ]; then
  openssl dgst -sha256 -sign "$SIGNING_KEY" -out publish/version.json.sig publish/version.json
else
  echo "서명 키가 없어 version.json에 서명하지 않았습니다 (--no-upload)."
fi
echo "빌드 완료: $VERSION (publish/$ZIP_NAME)"

if [ "$UPLOAD" -eq 0 ]; then
  exit 0
fi

upload() {
  scp -q -P "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 "$1" "$DEPLOY_USER@$DEPLOY_HOST:$WEB_DIR/.$2.uploading"
}

echo "업로드 중: $DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PORT"
"${SSH[@]}" "mkdir -p $WEB_DIR $RELEASE_DIR"
upload "publish/$ZIP_NAME" "$ZIP_NAME"
upload publish/version.json version.json
upload publish/version.json.sig version.json.sig

# zip을 먼저 자리에 놓고, 서명 → version.json 순서로 교체한다.
# 그 사이에 확인한 고객은 서명 검증에 실패해 지금 버전으로 실행되고 다음 실행 때 업데이트된다.
REMOTE_SHA=$("${SSH[@]}" "set -e
  cd $WEB_DIR
  chmod 644 .$ZIP_NAME.uploading .version.json.uploading .version.json.sig.uploading
  mv -f .$ZIP_NAME.uploading $ZIP_NAME
  cp $ZIP_NAME .BandProgram.zip.uploading
  mv -f .BandProgram.zip.uploading BandProgram.zip
  cp $ZIP_NAME $RELEASE_DIR/$ZIP_NAME
  mv -f .version.json.sig.uploading version.json.sig
  mv -f .version.json.uploading version.json
  sha256sum $ZIP_NAME | cut -d' ' -f1")
if [ "$SHA" != "$REMOTE_SHA" ]; then
  echo "업로드 확인 실패: 해시가 다릅니다 (로컬 $SHA, 서버 $REMOTE_SHA)" >&2
  exit 1
fi

# 서버가 지금 내려주는 version.json과 서명을 다시 받아 검증한다.
CHECK_DIR=$(mktemp -d)
trap 'rm -rf "$CHECK_DIR"' EXIT
"${SSH[@]}" "cat $WEB_DIR/version.json" > "$CHECK_DIR/version.json"
"${SSH[@]}" "cat $WEB_DIR/version.json.sig" > "$CHECK_DIR/version.json.sig"
openssl pkey -in "$SIGNING_KEY" -pubout -out "$CHECK_DIR/public.pem"
if ! openssl dgst -sha256 -verify "$CHECK_DIR/public.pem" -signature "$CHECK_DIR/version.json.sig" "$CHECK_DIR/version.json" >/dev/null; then
  echo "서버의 version.json 서명 검증 실패" >&2
  exit 1
fi

echo "업로드 완료: $VERSION"
echo "  다운로드:      $BASE_URL/BandProgram.zip"
echo "  업데이트 정보: $BASE_URL/version.json"
echo "  보관본:        $RELEASE_DIR/$ZIP_NAME"
