CREATE TABLE IF NOT EXISTS monsters (
    id           INT         NOT NULL PRIMARY KEY,
    name         VARCHAR(32) NOT NULL,
    max_hp       INT         NOT NULL,
    attack       INT         NOT NULL,
    defense      INT         NOT NULL,
    exp_reward   INT         NOT NULL,
    move_speed   INT         NOT NULL DEFAULT 1,
    aggro_range  INT         NOT NULL DEFAULT 192,
    respawn_sec  INT         NOT NULL DEFAULT 10
) ENGINE=InnoDB CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS monster_spawns (
    id           INT         NOT NULL AUTO_INCREMENT PRIMARY KEY,
    monster_id   INT         NOT NULL,
    x            INT         NOT NULL,
    y            INT         NOT NULL,
    CONSTRAINT fk_spawn_monster FOREIGN KEY (monster_id) REFERENCES monsters(id)
) ENGINE=InnoDB CHARSET=utf8mb4;

INSERT IGNORE INTO monsters (id,name,max_hp,attack,defense,exp_reward,move_speed,aggro_range,respawn_sec)
  VALUES (1, 'Slime', 50, 5, 0, 30, 1, 192, 10);

INSERT IGNORE INTO monster_spawns (id, monster_id, x, y) VALUES
  (1, 1, 200, 200),
  (2, 1, 300, 300),
  (3, 1, 500, 200),
  (4, 1, 600, 400),
  (5, 1, 100, 500);
