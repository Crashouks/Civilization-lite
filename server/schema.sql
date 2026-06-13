CREATE DATABASE IF NOT EXISTS civilization_lite
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

USE civilization_lite;

CREATE TABLE IF NOT EXISTS players (
  id INT AUTO_INCREMENT PRIMARY KEY,
  external_id VARCHAR(64) NOT NULL,
  display_name VARCHAR(128) NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uq_players_external_id (external_id)
);

CREATE TABLE IF NOT EXISTS game_saves (
  id INT AUTO_INCREMENT PRIMARY KEY,
  player_id INT NOT NULL,
  save_name VARCHAR(128) NOT NULL DEFAULT 'autosave',
  save_json LONGTEXT NOT NULL,
  turn_number INT NOT NULL DEFAULT 1,
  player_civ VARCHAR(64) NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  KEY idx_game_saves_player (player_id),
  KEY idx_game_saves_player_updated (player_id, updated_at),
  UNIQUE KEY uq_player_save_name (player_id, save_name),
  CONSTRAINT fk_game_saves_player
    FOREIGN KEY (player_id) REFERENCES players(id)
    ON DELETE CASCADE
);
