-- 2주차 스키마: 계정 / 월드 / 캐릭터
-- 적용: mysql -u mmorpg -p mmorpg2d < schema.sql

CREATE TABLE IF NOT EXISTS users (
    id            BIGINT      NOT NULL AUTO_INCREMENT PRIMARY KEY,
    login_id      VARCHAR(32) NOT NULL UNIQUE,
    pw_hash       VARCHAR(72) NOT NULL,
    created_at    DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at DATETIME    NULL
) ENGINE=InnoDB CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS worlds (
    id           INT         NOT NULL PRIMARY KEY,
    name         VARCHAR(32) NOT NULL,
    is_open      TINYINT(1)  NOT NULL DEFAULT 1,
    capacity     INT         NOT NULL DEFAULT 200
) ENGINE=InnoDB CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS characters (
    id           BIGINT      NOT NULL AUTO_INCREMENT PRIMARY KEY,
    account_id   BIGINT      NOT NULL,
    world_id     INT         NOT NULL,
    name         VARCHAR(16) NOT NULL UNIQUE,
    level        INT         NOT NULL DEFAULT 1,
    exp          BIGINT      NOT NULL DEFAULT 0,
    pos_x        INT         NOT NULL DEFAULT 400,
    pos_y        INT         NOT NULL DEFAULT 300,
    created_at   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_chars_account (account_id),
    CONSTRAINT fk_chars_user  FOREIGN KEY (account_id) REFERENCES users(id),
    CONSTRAINT fk_chars_world FOREIGN KEY (world_id)   REFERENCES worlds(id)
) ENGINE=InnoDB CHARSET=utf8mb4;

INSERT IGNORE INTO worlds (id, name, capacity) VALUES
  (1, '평원', 200),
  (2, '동굴', 100);
