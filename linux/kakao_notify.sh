#!/bin/bash

# 1. 설정
# 카카오 토큰이 저장된 JSON 파일의 경로
TOKEN_FILE="/usr/local/bin/kakao_token.json"
# 카카오 REST API 클라이언트 ID(API 키)
CLIENT_ID=""
# 토큰 갱신을 요청할 카카오 인증 서버 URL
REFRESH_URL="https://kauth.kakao.com/oauth/token"

# 2. 토큰 파일 읽기
# jq를 사용하여 파일에서 각 토큰 정보와 만료 시각을 읽어와 변수에 저장
ACCESS_TOKEN=$(jq -r '.access_token' "$TOKEN_FILE")
REFRESH_TOKEN=$(jq -r '.refresh_token' "$TOKEN_FILE")
EXPIRES_AT=$(jq -r '.expires_at' "$TOKEN_FILE")

# 3. 만료 확인 (사전 갱신)
# 현재 시각을 Unix 타임스탬프로 가져옴
NOW=$(date +%s)
# 현재 시각이 저장된 만료 시각보다 크거나 같은지 확인하여 토큰 만료 여부를 판단
if [[ "$NOW" -ge "$EXPIRES_AT" ]]; then
  # 3-1. 만료된 경우, Refresh Token을 사용하여 액세스 토큰 갱신 요청
  RESPONSE=$(curl -s -X POST "$REFRESH_URL" \
    -d "grant_type=refresh_token" \
    -d "client_id=$CLIENT_ID" \
    -d "refresh_token=$REFRESH_TOKEN")
  # 응답으로부터 새로운 액세스 토큰과 유효 기간을 추출
  NEW_ACCESS_TOKEN=$(echo "$RESPONSE" | jq -r .access_token)
  EXPIRES_IN=$(echo "$RESPONSE" | jq -r .expires_in)
  # 새 액세스 토큰 발급에 실패했는지 확인
  if [[ "$NEW_ACCESS_TOKEN" == "null" ]]; then
    echo "[ERROR] Access Token 갱신 실패"
    echo "$RESPONSE"
    exit 1
  fi
  # 새 만료 시각 계산 (현재 시각 + 유효 기간)
  NEW_EXPIRES_AT=$(( $(date +%s) + EXPIRES_IN ))
  # 새 리프레시 토큰 추출 (없으면 null이 됨)
  NEW_REFRESH_TOKEN=$(echo "$RESPONSE" | jq -r .refresh_token)
  # 만약 새 리프레시 토큰이 반환되지 않았다면, 기존 리프레시 토큰을 계속 사용
  if [[ "$NEW_REFRESH_TOKEN" == "null" ]]; then
    NEW_REFRESH_TOKEN=$REFRESH_TOKEN
  fi
  # jq를 사용하여 새로운 토큰 정보로 JSON 객체를 만들어 토큰 파일을 덮어씀
  jq -n \
      --arg at "$NEW_ACCESS_TOKEN" \
      --arg rt "$NEW_REFRESH_TOKEN" \
      --argjson exp $NEW_EXPIRES_AT \
      '{access_token:$at, refresh_token:$rt, expires_at:$exp}' > "$TOKEN_FILE"
  # 스크립트 내에서 사용할 변수들도 새로운 값으로 갱신
  ACCESS_TOKEN=$NEW_ACCESS_TOKEN
  REFRESH_TOKEN=$NEW_REFRESH_TOKEN
  EXPIRES_AT=$NEW_EXPIRES_AT
fi

# 4. 메시지 전송 (첫 번째 시도)
# 스크립트에 전달된 첫 번째 인자($1)를 메시지 내용으로 사용
MSG="$1"
# curl을 사용하여 '나에게 보내기' API 호출. Authorization 헤더에 액세스 토큰을 담아 전송.
RESPONSE=$(curl -s -X POST "https://kapi.kakao.com/v2/api/talk/memo/default/send" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d 'template_object={"object_type":"text","text":"'"${MSG}"'","link":{"web_url":"https://jongjinportfolio.com/"}}')

# 5. 만약 토큰 만료 에러라면, 한 번 더 갱신 & 재시도 (사후 갱신)
# 첫 시도 응답에 토큰 만료 관련 에러 메시지가 포함되어 있는지 정규식으로 확인
if [[ "$RESPONSE" =~ "access token is already expired" || "$RESPONSE" =~ "access token does not exist" ]]; then
  # 3-1과 동일한 로직으로 토큰을 다시 갱신
  RESPONSE2=$(curl -s -X POST "$REFRESH_URL" \
    -d "grant_type=refresh_token" \
    -d "client_id=$CLIENT_ID" \
    -d "refresh_token=$REFRESH_TOKEN")
  NEW_ACCESS_TOKEN=$(echo "$RESPONSE2" | jq -r .access_token)
  EXPIRES_IN=$(echo "$RESPONSE2" | jq -r .expires_in)
  # 갱신이 성공적으로 이루어졌다면
  if [[ "$NEW_ACCESS_TOKEN" != "null" ]]; then
    NEW_EXPIRES_AT=$(( $(date +%s) + EXPIRES_IN ))
    NEW_REFRESH_TOKEN=$(echo "$RESPONSE2" | jq -r .refresh_token)
    if [[ "$NEW_REFRESH_TOKEN" == "null" ]]; then
      NEW_REFRESH_TOKEN=$REFRESH_TOKEN
    fi
    # 토큰 파일과 스크립트 변수를 모두 갱신
    jq -n \
        --arg at "$NEW_ACCESS_TOKEN" \
        --arg rt "$NEW_REFRESH_TOKEN" \
        --argjson exp $NEW_EXPIRES_AT \
        '{access_token:$at, refresh_token:$rt, expires_at:$exp}' > "$TOKEN_FILE"
    ACCESS_TOKEN=$NEW_ACCESS_TOKEN
    
    # 갱신된 새 토큰으로 메시지 전송을 재시도
    RESPONSE=$(curl -s -X POST "https://kapi.kakao.com/v2/api/talk/memo/default/send" \
      -H "Authorization: Bearer $ACCESS_TOKEN" \
      -d 'template_object={"object_type":"text","text":"'"${MSG}"'","link":{"web_url":"https://jongjinportfolio.com/"}}')
  fi
fi

# 최종 응답 결과를 출력
echo "$RESPONSE"