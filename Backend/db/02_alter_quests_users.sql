-- =============================================================================
-- Sistema v2 - Fase 1: alteracoes em quests e users
-- =============================================================================
-- Torna as tarefas mensuraveis e verificaveis, liga cada quest ao objetivo que
-- a originou, substitui o flag is_penalty por um tipo (quest_kind) e adiciona a
-- mecanica financeira de faltas/divida ao jogador.
-- Rodar DEPOIS de 01_create_goals.sql.
-- Instrucoes individuais, sem COMMENT de coluna, para compatibilidade ampla
-- (phpMyAdmin / MySQL da Hostinger). Rode uma de cada vez se necessario.
-- =============================================================================

USE u224972953_org1;

-- ---------------------------------------------------------------------------
-- quests: vinculo com objetivo, metrica, tipo de prova, duracao e tipo da quest
-- ---------------------------------------------------------------------------
ALTER TABLE quests ADD COLUMN goal_id INT NULL AFTER user_id;

ALTER TABLE quests ADD COLUMN metric_target VARCHAR(255) NULL AFTER description;

ALTER TABLE quests ADD COLUMN proof_type ENUM('STRAVA','SUMMARY','MINDMAP','PHOTO','PAYMENT_RECEIPT','GENERIC') NOT NULL DEFAULT 'GENERIC' AFTER metric_target;

ALTER TABLE quests ADD COLUMN estimated_minutes INT NOT NULL DEFAULT 60 AFTER proof_type;

ALTER TABLE quests ADD COLUMN quest_kind ENUM('NORMAL','PENALTY','BONUS') NOT NULL DEFAULT 'NORMAL' AFTER is_penalty;

-- Converte o flag antigo para o novo enum.
UPDATE quests SET quest_kind = 'PENALTY' WHERE is_penalty = TRUE;

-- Liga quests ao objetivo (SET NULL se o objetivo for apagado).
ALTER TABLE quests ADD CONSTRAINT fk_quests_goal FOREIGN KEY (goal_id) REFERENCES goals(id) ON DELETE SET NULL;

-- Observacao: a coluna is_penalty fica mantida por compatibilidade nesta fase.
-- Pode ser removida numa limpeza futura:
--   ALTER TABLE quests DROP COLUMN is_penalty;

-- ---------------------------------------------------------------------------
-- users: mecanica financeira (faltas acumuladas e divida a doar)
-- ---------------------------------------------------------------------------
ALTER TABLE users ADD COLUMN failure_count INT NOT NULL DEFAULT 0;

ALTER TABLE users ADD COLUMN debt_amount DECIMAL(10,2) NOT NULL DEFAULT 0;
