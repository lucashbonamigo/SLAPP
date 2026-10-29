-- =============================================================================
-- Sistema v2 - Tabela de Punicoes (punishments)
-- =============================================================================
-- Catalogo controlado de punicoes. A punicao NAO e mais decidida pela IA:
-- o Worker sorteia uma punicao desta tabela quando o jogador falha.
-- A punicao FINANCEIRA tem peso menor (nao sai sempre).
-- Rodar DEPOIS de 02_alter_quests_users.sql.
-- =============================================================================

USE u224972953_org1;

CREATE TABLE IF NOT EXISTS punishments (
    id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(40) NOT NULL,
    category ENUM('FISICO','ROTINA','FINANCEIRO') NOT NULL,
    title VARCHAR(150) NOT NULL,
    description TEXT NOT NULL,
    proof_type ENUM('STRAVA','SUMMARY','MINDMAP','PHOTO','PAYMENT_RECEIPT','GENERIC') NOT NULL DEFAULT 'GENERIC',
    estimated_minutes INT NOT NULL DEFAULT 45,
    xp_reward INT NOT NULL DEFAULT 100,
    attribute_type ENUM('STR','INT','WIS','VIT') NOT NULL DEFAULT 'VIT',
    weight INT NOT NULL DEFAULT 10,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_punishments_code (code)
);

-- ---------------------------------------------------------------------------
-- Catalogo inicial (as 6 punicoes). weight alto = sai mais; FINANCEIRO baixo.
-- ---------------------------------------------------------------------------
INSERT INTO punishments (code, category, title, description, proof_type, estimated_minutes, xp_reward, attribute_type, weight) VALUES
('TAXA_MADRUGADA', 'FISICO',
 'A Taxa da Madrugada',
 'Antecipe o despertador para 03:30 (1h a menos de sono). A hora extra deve ser gasta em desconforto improdutivo: em pe no escuro ou alternando series de prancha isometrica em silencio, sem telas.',
 'PHOTO', 60, 120, 'VIT', 20),

('PUNICAO_CADEIRA', 'FISICO',
 'A Punicao da Cadeira',
 '45 minutos ininterruptos de alongamentos de maxima tensao em flexores de quadril e isquiotibiais. Mantenha cada posicao de dor suportavel por 5 min, sem musica, celular ou distracao.',
 'PHOTO', 45, 100, 'VIT', 20),

('SOBRECARGA_LIMIAR', 'FISICO',
 'Sobrecarga de Limiar',
 'Adicione ao proximo treino aerobico um bloco obrigatorio de 45 min em intensidade severa (Zona FC 4 ou 5), em corrida ou ciclismo.',
 'STRAVA', 60, 120, 'STR', 20),

('TRAJETO_VAZIO', 'ROTINA',
 'Trajeto no Vazio',
 'Faca todo o trajeto de ida e volta do dia seguinte em silencio absoluto: proibido podcast, pratica de ingles ou musica. O deslocamento e um exercicio mental de encarar a propria falha.',
 'GENERIC', 60, 80, 'WIS', 15),

('ISOLAMENTO_NOTURNO', 'ROTINA',
 'Isolamento Noturno',
 'Ao chegar em casa, fica revogado qualquer lazer, sofa ou interacao social ate as 21:30. O tempo deve ser gasto exclusivamente resolvendo o atraso gerado pela preguica anterior.',
 'GENERIC', 60, 80, 'WIS', 15),

('QUEIMA_RECURSOS', 'FINANCEIRO',
 'A Queima de Recursos',
 'Transfira o valor da divida acumulada para o pior destino possivel (instituicao ou causa que voce despreza) e anexe o comprovante para quitar o debito.',
 'PAYMENT_RECEIPT', 15, 50, 'WIS', 8);
