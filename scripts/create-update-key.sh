#!/usr/bin/env bash
# 자동 업데이트 서명 키(ECDSA P-256)를 한 번 만든다.
# 개인키는 이 맥에만 두고(저장소에 넣지 않는다), 출력되는 공개키를
# BandProgram.Core/Update/UpdatePublicKey.cs의 Base64 상수에 넣는다.
#
# 사용법: scripts/create-update-key.sh            새 키 만들기 (이미 있으면 멈춤)
#         scripts/create-update-key.sh --public   기존 키의 공개키만 출력
set -euo pipefail
KEY="${BAND_SIGNING_KEY:-$HOME/.config/band-deploy/update-signing-key.pem}"

if [ "${1:-}" = "--public" ]; then
  openssl pkey -in "$KEY" -pubout -outform DER | base64
  exit 0
fi

if [ -e "$KEY" ]; then
  echo "이미 서명 키가 있습니다: $KEY (덮어쓰지 않습니다)" >&2
  echo "공개키만 보려면: $0 --public" >&2
  exit 1
fi

mkdir -p "$(dirname "$KEY")"
chmod 700 "$(dirname "$KEY")"
(umask 077; openssl ecparam -name prime256v1 -genkey -noout -out "$KEY")
echo "개인키를 만들었습니다: $KEY"
echo "이 파일을 안전한 곳에 백업하세요. 잃어버리면 새 exe를 고객에게 직접 한 번 배포해야 합니다."
echo
echo "공개키 (UpdatePublicKey.cs의 Base64에 넣기):"
openssl pkey -in "$KEY" -pubout -outform DER | base64
