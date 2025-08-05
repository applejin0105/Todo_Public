#!/bin/bash

# --- 설정 ---
# 카카오 토큰이 저장된 JSON 파일의 경로
TOKEN_FILE="/usr/local/bin/kakao_token.json"
# 토큰 갱신을 담당하는 외부 스크립트의 경로
REFRESH_SCRIPT_PATH="/usr/local/bin/refresh_token.sh"
# '나에게 보내기' API의 엔드포인트 URL
MESSAGE_URL="https://kapi.kakao.com/v2/api/talk/memo/default/send"

# --- 1. 토큰 갱신 시도 ---
# 메시지 전송에 앞서, 토큰 갱신 스크립트를 먼저 실행하여 액세스 토큰이 유효하도록 보장
bash "$REFRESH_SCRIPT_PATH"

# --- 2. 최신 열쇠(액세스 토큰) 읽기 ---
# 갱신된 토큰 파일에서 액세스 토큰 값을 읽어옴
ACCESS_TOKEN=$(jq -r '.access_token' "$TOKEN_FILE")

# --- 3. 전달받은 소포(메시지) 전송 ---
# 이 스크립트에 전달된 첫 번째 인자($1)를 MESSAGE 변수에 저장
MESSAGE="$1"
# jq를 사용하여 카카오톡 메시지 템플릿 JSON 객체를 생성
# 참고: link의 web_url 값이 불완전함.
TEMPLATE_OBJECT=$(jq -n --arg msg "$MESSAGE" '{object_type:"text", text:$msg, link:{web_url:">
# curl을 사용하여 카카오 서버에 메시지 전송 API를 호출
# -H: HTTP 헤더 추가. Bearer 인증을 위해 액세스 토큰을 전달.
# -d: 폼 데이터 전송. 생성한 템플릿 객체를 전달.
RESPONSE=$(curl -s -X POST "$MESSAGE_URL" \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -d "template_object=$TEMPLATE_OBJECT")

# 메시지 전송 완료 후, 카카오 서버로부터 받은 응답을 출력

echo "메시지 전송 완료. 응답: $RESPONSE"
