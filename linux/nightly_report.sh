#!/bin/bash

# --- 설정 ---
# PostgreSQL 데이터베이스 사용자 이름
DB_USER=""
# PostgreSQL 비밀번호. 환경 변수로 설정하여 psql이 자동으로 사용하도록 함
export PGPASSWORD="!"
# 연결할 데이터베이스 이름
DB_NAME=""
# 메시지 전송을 담당하는 외부 스크립트의 경로
SEND_SCRIPT_PATH="/usr/local/bin/send_message.sh"

# --- 1. 보고서 내용(DB 쿼리) 작성 ---
# 어제 날짜를 "YYYY-MM-DD" 형식으로 생성하여 YESTERDAY 변수에 저장
YESTERDAY=$(date -d "yesterday" +%Y-%m-%d)
# psql 명령어를 사용하여 'TodoItems' 테이블에서 어제 완료된 항목들의 'Title'을 조회
# 참고: WHERE 절의 조건이 불완전하여 구문 오류가 발생할 수 있음.
COMPLETED_LIST=$(psql -U $DB_USER -d $DB_NAME -t -c "SELECT \"Title\" FROM \"TodoItems\" WHERE \"CompletedDate\"::date > 
# --- 2. 보고서 형식(메시지 포맷팅)---
# 최종적으로 전송될 메시지의 제목 부분 초기화. YESTERDAY 변수를 포함.
MESSAGE="🌙 $YESTERDAY 완료 작업 리포트"
# 줄 바꿈 추가
MESSAGE+=$'\n'
# COMPLETED_LIST 변수가 비어있는지 확인
if [ -z "$COMPLETED_LIST" ]; then
    # 비어있으면 "- 없음" 텍스트 추가
    MESSAGE+="- 없음"
else
    # 내용이 있으면, 각 줄의 시작(^)에 "✅ "를 추가하여 리스트 형식으로 만듦 (sed 명령어 사용)
    MESSAGE+=$(echo "$COMPLETED_LIST" | sed 's/^/✅ /')
fi

# 완성된 메시지를 인자로 하여 외부 메시지 전송 스크립트를 실행
bash "$SEND_SCRIPT_PATH" "$MESSAGE"