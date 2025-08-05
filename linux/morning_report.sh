#!/bin/bash

# --- 설정 ---
DB_USER=""
export PGPASSWORD=""
DB_NAME=""
SEND_SCRIPT_PATH="/usr/local/bin/send_message.sh"

# --- 1. 보고서 내용(DB 쿼리) 작성 ---
NOT_STARTED_LIST=$(psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Title\" FROM \"TodoItems\" WHERE \"Status\" = 0;")
IN_PROGRESS_LIST=$(psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Title\" FROM \"TodoItems\" WHERE \"Status\" = 1;")

# --- 2. 보고서 형식(메시지 포맷팅) 맞추기 ---
MESSAGE="📢 오늘 할 일 목록 (아침 브리핑)"
MESSAGE+=$'\n\n[미진행]\n'
if [ -z "$NOT_STARTED_LIST" ]; then MESSAGE+="- 없음"; else MESSAGE+=$(echo "$NOT_STARTED_LIST" | sed 's/^/ - /'); fi
MESSAGE+=$'\n\n[진행 중]\n'
if [ -z "$IN_PROGRESS_LIST" ]; then MESSAGE+="- 없음"; else MESSAGE+=$(echo "$IN_PROGRESS_LIST" | sed 's/^/ - /'); fi

# --- 3. 우편 배달부에게 보고서 전달 ---
bash "$SEND_SCRIPT_PATH" "$MESSAGE"