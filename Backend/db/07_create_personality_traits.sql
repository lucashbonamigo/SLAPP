-- =============================================================================
-- Sistema v2 - Fase 1: Tracos de Personalidade (Big Five)
-- =============================================================================
-- Rastreia a evolucao da personalidade do jogador segundo o Modelo dos Cinco
-- Grandes Fatores (Big Five / OCEAN), uma teoria amplamente aceita em psicologia.
-- A IA ajusta estes valores a cada avaliacao noturna conforme o jogador age
-- (cumpre tarefas sob pressao, reflete, evita o piloto automatico).
--
--   OPENNESS          = Abertura a Experiencia (curiosidade, criatividade)
--   CONSCIENTIOUSNESS = Conscienciosidade (disciplina, organizacao, foco)
--   EXTRAVERSION      = Extroversao (assertividade, exposicao social)
--   AGREEABLENESS     = Amabilidade (cooperacao, empatia)
--   NEUROTICISM       = Neuroticismo (reatividade ao estresse; MENOR e melhor)
--
-- value: 0-100 (media movel mantida pela IA). Comeca em 50 (neutro).
-- Rodar DEPOIS de 05_add_user_password.sql.
-- =============================================================================

USE u224972953_org1;

-- ---------------------------------------------------------------------------
-- Estado atual de cada traco (uma linha por jogador por traco).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS personality_traits (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,

    trait_code ENUM('OPENNESS','CONSCIENTIOUSNESS','EXTRAVERSION','AGREEABLENESS','NEUROTICISM') NOT NULL
        COMMENT 'Fator do Big Five',
    value DECIMAL(5,2) NOT NULL DEFAULT 50.00
        COMMENT 'Media movel 0-100 mantida pela IA. 50 = neutro',

    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uq_trait_user_code (user_id, trait_code)
);

-- ---------------------------------------------------------------------------
-- Historico diario: um snapshot por traco por dia, para plotar o radar ao
-- longo do tempo e comparar com a silhueta "Nivel 100".
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS personality_trait_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,

    trait_code ENUM('OPENNESS','CONSCIENTIOUSNESS','EXTRAVERSION','AGREEABLENESS','NEUROTICISM') NOT NULL,
    value DECIMAL(5,2) NOT NULL
        COMMENT 'Valor do traco na data do snapshot',
    delta DECIMAL(5,2) NOT NULL DEFAULT 0
        COMMENT 'Variacao aplicada neste dia pela IA',

    recorded_at DATE NOT NULL
        COMMENT 'Dia do snapshot',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_trait_hist_user_date (user_id, recorded_at),
    UNIQUE KEY uq_trait_hist_day (user_id, trait_code, recorded_at)
);

-- ---------------------------------------------------------------------------
-- Semeia os 5 tracos em 50 (neutro) para o jogador 1. Idempotente.
-- Para outros jogadores, repita trocando o user_id.
-- ---------------------------------------------------------------------------
INSERT INTO personality_traits (user_id, trait_code, value)
SELECT 1, t.code, 50.00
FROM (
    SELECT 'OPENNESS'          AS code UNION ALL
    SELECT 'CONSCIENTIOUSNESS'      UNION ALL
    SELECT 'EXTRAVERSION'           UNION ALL
    SELECT 'AGREEABLENESS'          UNION ALL
    SELECT 'NEUROTICISM'
) AS t
ON DUPLICATE KEY UPDATE value = personality_traits.value;
