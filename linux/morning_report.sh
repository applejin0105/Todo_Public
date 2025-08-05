#!/bin/bash

# --- 설정 ---
# PostgreSQL 데이터베이스 사용자 이름
DB_USER=""
# PostgreSQL 비밀번호. 환경 변수로 설정하여 psql이 자동으로 사용하도록 함
export PGPASSWORD=""
# 연결할 데이터베이스 이름
DB_NAME=""
# 메시지 전송을 담당하는 외부 스크립트의 경로
SEND_SCRIPT_PATH="/usr/local/bin/send_message.sh"

# --- 1. 보고서 내용(DB 쿼리) 작성 ---
# psql 명령어를 사용하여 'TodoItems' 테이블에서 'Status'가 0 (미진행)인 항목들의 'Title'을 조회
# -U: 사용자 지정, -d: 데이터베이스 지정, -t: 튜플 전용 모드 (헤더와 푸터 없이 데이터만 출력)
NOT_STARTED_LIST=$(psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Title\" FROM \"TodoItems\" WHERE \"Status\" = 0;")
# 'Status'가 1 (진행 중)인 항목들의 'Title'을 조회
IN_PROGRESS_LIST=$(psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Title\" FROM \"TodoItems\" WHERE \"Status\" = 1;")

# --- 2. 보고서 형식(메시지 포맷팅) 맞추기 ---
# 최종적으로 전송될 메시지의 제목 부분 초기화
MESSAGE="📢 오늘 할 일 목록 (아침 브리핑)"
# 줄 바꿈(\n)을 포함하여 메시지에 [미진행] 섹션 추가
MESSAGE+=$'\n\n[미진행]\n'
# NOT_STARTED_LIST 변수가 비어있는지 확인 (-z)
if [ -z "$NOT_STARTED_LIST" ]; then
    # 비어있으면 "- 없음" 텍스트 추가
    MESSAGE+="- 없음"
else
    # 내용이 있으면, 각 줄의 시작(^)에 " - "를 추가하여 리스트 형식으로 만듦 (sed 명령어 사용)
    MESSAGE+=$(echo "$NOT_STARTED_LIST" | sed 's/^/ - /')
fi
# [진행 중] 섹션 추가 및 위와 동일한 로직으로 포맷팅
MESSAGE+=$'\n\n[진행 중]\n'
if [ -z "$IN_PROGRESS_LIST" ]; then MESSAGE+="- 없음"; else MESSAGE+=$(echo "$IN_PROGRESS_LIST" | sed 's/^/ - /'); fi

# --- 3. 우편 배달부에게 보고서 전달 ---
# 완성된 메시지를 인자로 하여 외부 메시지 전송 스크립트를 실행
bash "$SEND_SCRIPT_PATH" "$MESSAGE"