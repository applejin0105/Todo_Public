#!/bin/bash

# --- 설정 ---
TOKEN_FILE="/usr/local/bin/kakao_token.json"
CLIENT_ID=""
REFRESH_URL="https://kauth.kakao.com/oauth/token"

# --- 토큰 파일 존재 확인 ---
if [ ! -f "$TOKEN_FILE" ]; then
    echo "오류: 토큰 파일($TOKEN_FILE)이 없습니다. 최초 인증이 필요합니다."
    exit 1
fi

# --- 토큰 정보 읽기 ---
REFRESH_TOKEN=$(jq -r '.refresh_token' "$TOKEN_FILE")
EXPIRES_AT=$(jq -r '.expires_at' "$TOKEN_FILE")
NOW=$(date +%s)

# --- 만료 여부 확인 및 갱신 ---
if [[ "$NOW" -ge "$EXPIRES_AT" ]]; then
    echo "토큰이 만료되어 갱신을 시도합니다..."
    RESPONSE=$(curl -s -X POST "$REFRESH_URL" \
        -d "grant_type=refresh_token" \
        -d "client_id=$CLIENT_ID" \
        -d "refresh_token=$REFRESH_TOKEN")

    NEW_ACCESS_TOKEN=$(echo "$RESPONSE" | jq -r .access_token)

    if [[ "$NEW_ACCESS_TOKEN" == "null" ]]; then
        echo "오류: 액세스 토큰 갱신에 실패했습니다."
        echo "응답: $RESPONSE"
        exit 1
    fi

    EXPIRES_IN=$(echo "$RESPONSE" | jq -r .expires_in)
    NEW_EXPIRES_AT=$(( $(date +%s) + EXPIRES_IN - 300 ))

    # ▼▼▼ 이 부분의 jq 표현식을 작은따옴표로 감쌌습니다. ▼▼▼
    NEW_REFRESH_TOKEN=$(echo "$RESPONSE" | jq -r '.refresh_token // empty')

    if [[ -z "$NEW_REFRESH_TOKEN" ]]; then
        NEW_REFRESH_TOKEN=$REFRESH_TOKEN
    fi

    jq -n \
      --arg at "$NEW_ACCESS_TOKEN" \
      --arg rt "$NEW_REFRESH_TOKEN" \
      --argjson exp $NEW_EXPIRES_AT \
      '{access_token:$at, refresh_token:$rt, expires_at:$exp}' > "$TOKEN_FILE"

    echo "토큰이 성공적으로 갱신되었습니다."
else
    echo "토큰이 아직 유효합니다."
fi