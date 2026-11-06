-- =============================================================================
-- Sistema v2 - Fase 1: Metricas das Missoes (quest_metrics)
-- =============================================================================
-- O "Motor de 1% ao dia". Em vez de registrar o progresso apenas como texto
-- livre em goals.progress_notes, cada missao aprovada passa a gerar dados
-- numericos chave-valor (pace, bpm, distancia, minutos de foco, paginas lidas).
-- Esta tabela e a fonte das series temporais usadas nos graficos do frontend e
-- na analise de platos pela IA.
-- Rodar DEPOIS de 02_alter_quests_users.sql.
-- =============================================================================

USE u224972953_org1;

CREATE TABLE IF NOT EXISTS quest_metrics (
    id INT AUTO_INCREMENT PRIMARY KEY,
    quest_id INT NOT NULL
        COMMENT 'Missao que produziu a metrica',
    goal_id INT NULL
        COMMENT 'Objetivo macro associado (desnormalizado p/ consultas de serie temporal)',
    user_id INT NOT NULL
        COMMENT 'Dono da metrica (desnormalizado p/ consultas rapidas por jogador)',

    metric_name VARCHAR(50) NOT NULL
        COMMENT 'Chave da metrica. Ex: pace_medio, bpm_medio, distancia_km, foco_min, paginas',
    metric_value DECIMAL(10,2) NOT NULL
        COMMENT 'Valor numerico plotavel',
    unit VARCHAR(20) NULL
        COMMENT 'Unidade legivel. Ex: km, min/km, bpm, min, pag',

    recorded_at DATE NOT NULL
        COMMENT 'Data a que a metrica se refere (normalmente o target_date da quest)',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (quest_id) REFERENCES quests(id) ON DELETE CASCADE,
    FOREIGN KEY (goal_id) REFERENCES goals(id) ON DELETE SET NULL,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,

    INDEX idx_metrics_goal_name_date (goal_id, metric_name, recorded_at),
    INDEX idx_metrics_user_date (user_id, recorded_at)
);
