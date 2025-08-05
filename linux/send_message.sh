#!/bin/bash

# --- 설정 ---
TOKEN_FILE="/usr/local/bin/kakao_token.json"
REFRESH_SCRIPT_PATH="/usr/local/bin/refresh_token.sh"
MESSAGE_URL="https://kapi.kakao.com/v2/api/talk/memo/default/send"

# --- 1. 토큰 갱신 시도 ---
bash "$REFRESH_SCRIPT_PATH"

# --- 2. 최신 열쇠(액세스 토큰) 읽기 ---
ACCESS_TOKEN=$(jq -r '.access_token' "$TOKEN_FILE")

# --- 3. 전달받은 소포(메시지) 전송 ---
MESSAGE="$1"
TEMPLATE_OBJECT=$(jq -n --arg msg "$MESSAGE" '{object_type:"text", text:$msg, link:{web_url:"https://jongjinportfolio.c>
RESPONSE=$(curl -s -X POST "$MESSAGE_URL" \
     -H "Authorization: Bearer $ACCESS_TOKEN" \
     -d "template_object=$TEMPLATE_OBJECT")

echo "메시지 전송 완료. 응답: $RESPONSE"