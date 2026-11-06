-- =============================================================================
-- Sistema v2 - Fase 2: Reflexao Noturna (daily_reflections)
-- =============================================================================
-- O "Diario de Piloto Automatico" analisado pela IA. Antes de dormir, o jogador
-- registra duas variaveis sobre o dia:
--   1. Como reagiu no piloto automatico diante de um desafio/estresse.
--   2. Como o 'Eu Nivel 100' teria reagido.
-- O Mentor (IA) le isto na avaliacao noturna, devolve uma reestruturacao
-- cognitiva (mentor_feedback) e ajusta os tracos Big Five de acordo.
-- Uma reflexao por jogador por dia.
-- Rodar DEPOIS de 07_create_personality_traits.sql.
-- =============================================================================

USE u224972953_org1;

CREATE TABLE IF NOT EXISTS daily_reflections (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,

    reflection_date DATE NOT NULL
        COMMENT 'Dia a que a reflexao se refere',

    autopilot_text TEXT NOT NULL
        COMMENT 'Situacao de estresse/desafio e como o jogador reagiu no piloto automatico',
    ideal_text TEXT NULL
        COMMENT 'Como o Eu Nivel 100 teria reagido (resposta deliberada desejada)',

    mentor_feedback TEXT NULL
        COMMENT 'Reestruturacao cognitiva devolvida pela IA na avaliacao noturna',

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    UNIQUE KEY uq_reflection_user_day (user_id, reflection_date),
    INDEX idx_reflection_user_date (user_id, reflection_date)
);
