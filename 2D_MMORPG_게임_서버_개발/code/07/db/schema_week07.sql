CREATE TABLE IF NOT EXISTS item_defs (
    id          INT NOT NULL PRIMARY KEY,
    name        VARCHAR(32) NOT NULL,
    type        TINYINT NOT NULL,
    effect_kind TINYINT NOT NULL,
    effect_amt  INT NOT NULL,
    max_stack   INT NOT NULL DEFAULT 1
) ENGINE=InnoDB CHARSET=utf8mb4;

INSERT IGNORE INTO item_defs (id, name, type, effect_kind, effect_amt, max_stack)
  VALUES (1001, 'Small Potion', 1, 1, 30, 99);

CREATE TABLE IF NOT EXISTS inventory (
    id           BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    character_id BIGINT NOT NULL,
    slot_idx     INT    NOT NULL,
    item_def_id  INT    NOT NULL,
    qty          INT    NOT NULL,
    INDEX idx_inv_char (character_id),
    UNIQUE KEY uk_inv_slot (character_id, slot_idx),
    CONSTRAINT fk_inv_char FOREIGN KEY (character_id) REFERENCES characters(id),
    CONSTRAINT fk_inv_def  FOREIGN KEY (item_def_id) REFERENCES item_defs(id)
) ENGINE=InnoDB CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS monster_drops (
    id          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    monster_id  INT NOT NULL,
    item_def_id INT NOT NULL,
    chance      INT NOT NULL,
    qty_min     INT NOT NULL DEFAULT 1,
    qty_max     INT NOT NULL DEFAULT 1
) ENGINE=InnoDB CHARSET=utf8mb4;

INSERT IGNORE INTO monster_drops (id, monster_id, item_def_id, chance, qty_min, qty_max)
  VALUES (1, 1, 1001, 5000, 1, 1);
