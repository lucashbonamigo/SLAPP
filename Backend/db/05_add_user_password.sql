-- =============================================================================
-- Sistema v2 - Senha por usuario
-- =============================================================================
-- Adiciona o hash de senha na tabela users para suportar cadastro de multiplos
-- usuarios (cada um com sua propria senha, via BCrypt). Usuarios antigos ficam
-- com password_hash NULL e continuam entrando pela senha compartilhada
-- (ApiAuth:Password) ate definirem uma senha propria.
-- Rodar DEPOIS de 02_alter_quests_users.sql.
-- =============================================================================

USE u224972953_org1;

ALTER TABLE users ADD COLUMN password_hash VARCHAR(255) NULL AFTER email;
