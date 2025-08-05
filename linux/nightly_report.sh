#!/bin/bash

# --- 설정 ---
DB_USER=""
export PGPASSWORD="!"
DB_NAME=""
SEND_SCRIPT_PATH="/usr/local/bin/send_message.sh"

# --- 1. 보고서 내용(DB 쿼리) 작성 ---
YESTERDAY=$(date -d "yesterday" +%Y-%m-%d)
COMPLETED_LIST=$(psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Title\" FROM \"TodoItems\" WHERE \"CompletedDate\"::date >
# --- 2. 보고서 형식(메시지 포맷팅)---
MESSAGE="🌙 $YESTERDAY 완료 작업 리포트"
MESSAGE+=$'\n'
if [ -z "$COMPLETED_LIST" ]; then MESSAGE+="- 없음"; else MESSAGE+=$(echo "$COMPLETED_LIST" | sed 's/^/✅ /'); fi

bash "$SEND_SCRIPT_PATH" "$MESSAGE"