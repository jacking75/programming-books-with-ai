-- Week 5 schema patch: add combat stats to characters
ALTER TABLE characters
  ADD COLUMN hp      INT NOT NULL DEFAULT 100 AFTER exp,
  ADD COLUMN max_hp  INT NOT NULL DEFAULT 100 AFTER hp,
  ADD COLUMN attack  INT NOT NULL DEFAULT 10  AFTER max_hp,
  ADD COLUMN defense INT NOT NULL DEFAULT 0   AFTER attack;
