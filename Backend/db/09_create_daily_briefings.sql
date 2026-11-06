-- =============================================================================
-- Sistema v2 - Fase 3: Briefing Diario de Noticias (daily_briefings)
-- =============================================================================
-- O "Daily Briefing" matinal. Um Background Service busca manchetes de feeds RSS
-- publicos (Hacker News, TechCrunch, G1, Reuters...), a IA filtra e resume o que
-- afeta TI, C#/.NET, economia e geopolitica em poucos topicos. O jogador le isto
-- de manha para nao ficar desatualizado, sem rolar feeds.
-- O briefing e global (single-player v2): um por dia.
-- Rodar DEPOIS de 08_create_daily_reflections.sql.
-- =============================================================================

USE u224972953_org1;

CREATE TABLE IF NOT EXISTS daily_briefings (
    id INT AUTO_INCREMENT PRIMARY KEY,

    briefing_date DATE NOT NULL
        COMMENT 'Dia do briefing (um por dia)',

    summary_md TEXT NOT NULL
        COMMENT 'Resumo em markdown/bullets gerado pela IA',
    source_count INT NOT NULL DEFAULT 0
        COMMENT 'Quantas manchetes alimentaram o resumo',

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE KEY uq_briefing_day (briefing_date)
);
