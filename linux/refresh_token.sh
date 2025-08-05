#!/bin/bash

# --- 설정 ---
# 카카오 토큰이 저장된 JSON 파일의 경로
TOKEN_FILE="/usr/local/bin/kakao_token.json"
# 카카오 REST API 클라이언트 ID(API 키)
CLIENT_ID=""
# 토큰 갱신을 요청할 카카오 인증 서버 URL
REFRESH_URL="https://kauth.kakao.com/oauth/token"

# --- 토큰 파일 존재 확인 ---
# -f: 파일이 존재하고 일반 파일인지 확인
if [ ! -f "$TOKEN_FILE" ]; then
    echo "오류: 토큰 파일($TOKEN_FILE)이 없습니다. 최초 인증이 필요합니다."
    exit 1 # 스크립트 비정상 종료
fi

# --- 토큰 정보 읽기 ---
# jq: JSON 파서. -r 옵션은 raw output(따옴표 없이)을 의미. .refresh_token 키의 값을 읽어옴.
REFRESH_TOKEN=$(jq -r '.refresh_token' "$TOKEN_FILE")
# .expires_at 키의 값을 읽어옴 (Unix 타임스탬프)
EXPIRES_AT=$(jq -r '.expires_at' "$TOKEN_FILE")
# 현재 시간을 Unix 타임스탬프(1970년 1월 1일 이후 경과된 초)로 가져옴
NOW=$(date +%s)

# --- 만료 여부 확인 및 갱신 ---
# 현재 시간이 만료 시간보다 크거나 같은지(-ge) 확인
if [[ "$NOW" -ge "$EXPIRES_AT" ]]; then
    echo "토큰이 만료되어 갱신을 시도합니다..."
    # curl: HTTP 요청을 보내는 도구. -s(silent) 옵션으로 진행률 표시 숨김, -X POST로 POST 요청 지정, -d로 폼 데이터 전송
    RESPONSE=$(curl -s -X POST "$REFRESH_URL" \
        -d "grant_type=refresh_token" \
        -d "client_id=$CLIENT_ID" \
        -d "refresh_token=$REFRESH_TOKEN")

    # 응답(RESPONSE)에서 새로운 액세스 토큰을 추출
    NEW_ACCESS_TOKEN=$(echo "$RESPONSE" | jq -r .access_token)

    # 추출한 새 액세스 토큰이 "null" 문자열이면 갱신 실패로 간주
    if [[ "$NEW_ACCESS_TOKEN" == "null" ]]; then
        echo "오류: 액세스 토큰 갱신에 실패했습니다."
        echo "응답: $RESPONSE"
        exit 1
    fi

    # 응답에서 새 토큰의 유효 기간(초)을 추출
    EXPIRES_IN=$(echo "$RESPONSE" | jq -r .expires_in)
    # 새 만료 시각 계산: 현재시간 + 유효기간 - 300초(5분 여유)
    NEW_EXPIRES_AT=$(( $(date +%s) + EXPIRES_IN - 300 ))

    # 응답에서 새 리프레시 토큰을 추출 시도. 없으면(null) 빈 문자열 반환 (// empty)
    NEW_REFRESH_TOKEN=$(echo "$RESPONSE" | jq -r '.refresh_token // empty')

    # 만약 새 리프레시 토큰이 반환되지 않았다면, 기존 리프레시 토큰을 계속 사용
    if [[ -z "$NEW_REFRESH_TOKEN" ]]; then
        NEW_REFRESH_TOKEN=$REFRESH_TOKEN
    fi

    # jq를 사용하여 새로운 JSON 객체를 생성하고 토큰 파일을 덮어씀
    # -n: 입력 없이 JSON 생성, --arg/--argjson: 변수를 jq 내부 변수로 전달
    jq -n \
      --arg at "$NEW_ACCESS_TOKEN" \
      --arg rt "$NEW_REFRESH_TOKEN" \
      --argjson exp $NEW_EXPIRES_AT \
      '{access_token:$at, refresh_token:$rt, expires_at:$exp}' > "$TOKEN_FILE"

    echo "토큰이 성공적으로 갱신되었습니다."
else
    echo "토큰이 아직 유효합니다."
fi