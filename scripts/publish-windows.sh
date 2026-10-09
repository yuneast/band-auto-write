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
# 사용법: scripts/publish-windows.sh [--no-upload | --upload-only]
#   --no-upload    빌드와 서명만 한다 (Windows에서 시험할 때)
#   --upload-only  이미 만든 publish/ 결과물을 (시험한 그대로) 검증 후 업로드한다
set -euo pipefail
cd "$(dirname "$0")/.."

UPLOAD=1
UPLOAD_ONLY=0
for arg in "$@"; do
  case "$arg" in
    --no-upload) UPLOAD=0 ;;
    --upload-only) UPLOAD_ONLY=1 ;;
    *) echo "알 수 없는 옵션: $arg (사용법: $0 [--no-upload | --upload-only])" >&2; exit 2 ;;
  esac
done
if [ "$UPLOAD" -eq 0 ] && [ "$UPLOAD_ONLY" -eq 1 ]; then
  echo "--no-upload와 --upload-only는 함께 쓸 수 없습니다." >&2
  exit 2
fi

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

# 앱에 들어간 공개키와 서명 키가 같은지 확인한다. 다르면 고객의 자동 업데이트가 끊긴다.
if [ -f "$SIGNING_KEY" ]; then
  APP_KEY=$(sed -n 's/.*public const string Base64 = "\([^"]*\)".*/\1/p' BandProgram.Core/Update/UpdatePublicKey.cs | head -1)
  if [ -z "$APP_KEY" ]; then
    echo "UpdatePublicKey.cs에서 공개키를 읽지 못했습니다." >&2
    exit 1
  fi
  KEY_PUB=$(openssl pkey -in "$SIGNING_KEY" -pubout -outform DER | base64 | tr -d '\n')
  if [ "$APP_KEY" != "$KEY_PUB" ]; then
    echo "앱에 들어간 공개키(UpdatePublicKey.cs)와 서명 키가 다릅니다. 이대로 배포하면 고객의 자동 업데이트가 끊깁니다." >&2
    exit 1
  fi
fi

check_remote_version() {
  local remote_version
  remote_version=$("${SSH[@]}" "cat $WEB_DIR/version.json 2>/dev/null || true" | sed -n 's/.*"version" *: *"\([0-9.]*\)".*/\1/p')
  if [ -n "$remote_version" ] && ! version_gt "$VERSION" "$remote_version"; then
    echo "서버 버전($remote_version)보다 높지 않아 배포하지 않습니다: $VERSION (새로 빌드해서 다시 올리세요)" >&2
    exit 1
  fi
}

upload() {
  scp -q -P "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=10 "$1" "$DEPLOY_USER@$DEPLOY_HOST:$WEB_DIR/.$2.uploading"
}

# publish/의 결과물을 서버에 올리고 검증한다. VERSION, ZIP_NAME, SHA 필요.
upload_release() {
  echo "업로드 중: $DEPLOY_USER@$DEPLOY_HOST:$DEPLOY_PORT"
  "${SSH[@]}" "mkdir -p $WEB_DIR $RELEASE_DIR"
  upload "publish/$ZIP_NAME" "$ZIP_NAME"
  upload publish/version.json version.json
  upload publish/version.json.sig version.json.sig

  # 서버에 올라간 zip의 해시를 먼저 확인한다. 다르면 아무것도 공개하지 않고 지운다.
  # 맞으면 zip을 먼저 자리에 놓고, 서명 → version.json 순서로 교체한다.
  # 그 사이에 확인한 고객은 서명 검증에 실패해 지금 버전으로 실행되고 다음 실행 때 업데이트된다.
  local remote_sha
  remote_sha=$("${SSH[@]}" "set -e
  cd $WEB_DIR
  UP_SHA=\$(sha256sum .$ZIP_NAME.uploading | cut -d' ' -f1)
  if [ \"\$UP_SHA\" != \"$SHA\" ]; then
    rm -f .$ZIP_NAME.uploading .version.json.uploading .version.json.sig.uploading
    echo \"업로드된 zip의 해시가 다릅니다 (서버 \$UP_SHA)\" >&2
    exit 1
  fi
  chmod 644 .$ZIP_NAME.uploading .version.json.uploading .version.json.sig.uploading
  mv -f .$ZIP_NAME.uploading $ZIP_NAME
  cp $ZIP_NAME .BandProgram.zip.uploading
  mv -f .BandProgram.zip.uploading BandProgram.zip
  cp $ZIP_NAME $RELEASE_DIR/$ZIP_NAME
  mv -f .version.json.sig.uploading version.json.sig
  mv -f .version.json.uploading version.json
  sha256sum $ZIP_NAME | cut -d' ' -f1")
  if [ "$SHA" != "$remote_sha" ]; then
    echo "업로드 확인 실패: 해시가 다릅니다 (로컬 $SHA, 서버 $remote_sha)" >&2
    exit 1
  fi

  # 서버가 지금 내려주는 version.json과 서명을 다시 받아 검증한다.
  local check_dir
  check_dir=$(mktemp -d)
  "${SSH[@]}" "cat $WEB_DIR/version.json" > "$check_dir/version.json"
  "${SSH[@]}" "cat $WEB_DIR/version.json.sig" > "$check_dir/version.json.sig"
  openssl pkey -in "$SIGNING_KEY" -pubout -out "$check_dir/public.pem"
  if ! openssl dgst -sha256 -verify "$check_dir/public.pem" -signature "$check_dir/version.json.sig" "$check_dir/version.json" >/dev/null; then
    rm -rf "$check_dir"
    echo "서버의 version.json 서명 검증 실패" >&2
    exit 1
  fi
  rm -rf "$check_dir"

  echo "업로드 완료: $VERSION"
  echo "  다운로드:      $BASE_URL/BandProgram.zip"
  echo "  업데이트 정보: $BASE_URL/version.json"
  echo "  보관본:        $RELEASE_DIR/$ZIP_NAME"
}

if [ "$UPLOAD_ONLY" -eq 1 ]; then
  if [ ! -f publish/version.json ]; then
    echo "publish/version.json이 없습니다. 먼저 --no-upload로 빌드하세요." >&2
    exit 1
  fi
  VERSION=$(sed -n 's/.*"version" *: *"\([0-9.]*\)".*/\1/p' publish/version.json | head -1)
  if [ -z "$VERSION" ]; then
    echo "publish/version.json에서 버전을 읽지 못했습니다." >&2
    exit 1
  fi
  ZIP_NAME="BandProgram-$VERSION.zip"
  for f in "publish/$ZIP_NAME" publish/BandProgram.zip publish/version.json publish/version.json.sig; do
    if [ ! -f "$f" ]; then
      echo "필요한 파일이 없습니다: $f (먼저 --no-upload로 빌드하세요)" >&2
      exit 1
    fi
  done
  SHA=$(shasum -a 256 "publish/$ZIP_NAME" | cut -d' ' -f1)
  MANIFEST_SHA=$(sed -n 's/.*"sha256" *: *"\([0-9a-f]*\)".*/\1/p' publish/version.json | head -1)
  if [ "$SHA" != "$MANIFEST_SHA" ]; then
    echo "publish/$ZIP_NAME의 해시가 version.json과 다릅니다 (zip $SHA, version.json $MANIFEST_SHA)" >&2
    exit 1
  fi
  if [ "$(shasum -a 256 publish/BandProgram.zip | cut -d' ' -f1)" != "$SHA" ]; then
    echo "publish/BandProgram.zip이 publish/$ZIP_NAME과 다릅니다." >&2
    exit 1
  fi
  LOCAL_CHECK=$(mktemp -d)
  openssl pkey -in "$SIGNING_KEY" -pubout -out "$LOCAL_CHECK/public.pem"
  if ! openssl dgst -sha256 -verify "$LOCAL_CHECK/public.pem" -signature publish/version.json.sig publish/version.json >/dev/null; then
    rm -rf "$LOCAL_CHECK"
    echo "publish/version.json 서명 검증 실패 (파일이 바뀌었거나 다른 키로 서명됨)" >&2
    exit 1
  fi
  rm -rf "$LOCAL_CHECK"
  echo "로컬 확인 완료: $VERSION (해시, 서명 일치)"
  check_remote_version
  upload_release
  exit 0
fi

VERSION=$(date +%Y.%m.%d.%H%M | awk -F. '{ printf "%d.%d.%d.%d", $1, $2, $3, $4 }')
ZIP_NAME="BandProgram-$VERSION.zip"

if [ "$UPLOAD" -eq 1 ]; then
  check_remote_version
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

upload_release
