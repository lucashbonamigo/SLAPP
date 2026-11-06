-- =============================================================================
-- Sistema v2 - Fase 3 (ajuste): itens estruturados do briefing com link
-- =============================================================================
-- Guarda as manchetes curadas pela IA como JSON (titulo, fonte, url, comentario),
-- preservando o LINK da fonte original. A coluna summary_md permanece como
-- fallback/compatibilidade.
-- Rodar DEPOIS de 09_create_daily_briefings.sql.
-- =============================================================================

USE u224972953_org1;

ALTER TABLE daily_briefings
    ADD COLUMN items_json TEXT NULL
        COMMENT 'Itens curados em JSON: [{source,title,url,comment}]'
        AFTER summary_md;
