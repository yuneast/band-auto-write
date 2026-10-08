-- Band Auto Write 애플리케이션을 위한 SQLite 테이블 생성 스크립트
-- 사용법: sqlite3 data/newsoft.sqlite < create_tables.sql

CREATE TABLE user (
  id TEXT PRIMARY KEY,           -- 사용자 아이디
  pass TEXT NOT NULL,            -- 비밀번호 (login.php 비교 로직에 맞춰 소문자 평문 저장)
  date TEXT NOT NULL,            -- 서비스 만료일 (YYYY-MM-DD)
  band_session TEXT,             -- Band 세션 토큰
  fb_session TEXT,               -- Facebook 세션 토큰
  is_admin INTEGER NOT NULL DEFAULT 0,  -- 관리자 여부
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_user_date ON user (date);

-- 계정 데이터
INSERT INTO user (id, pass, date, is_admin) VALUES
('mcfly17', 'ehdwns2510@', '2099-12-31', 1),
('hmc0413', 'alswl5159', '2026-09-05', 0),
('pcmy88', 'champ88', '2026-10-21', 0);
