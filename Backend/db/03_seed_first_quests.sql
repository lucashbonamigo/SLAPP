-- =============================================================================
-- Sistema v2 - Seed das 3 primeiras quests (uma por objetivo)
-- =============================================================================
-- Primeiro passo de cada objetivo ativo, no formato novo (metric_target,
-- proof_type, estimated_minutes, quest_kind, goal_id). Com target_date = HOJE
-- para aparecerem no painel e serem avaliadas na proxima auditoria.
-- O goal_id e resolvido por subconsulta pelo titulo, entao funciona
-- independente dos ids reais. Assume user_id = 1.
-- =============================================================================

USE u224972953_org1;

-- 1) Meia maratona (RUNNING -> Strava)
INSERT INTO quests
    (user_id, goal_id, title, description, metric_target, proof_type,
     estimated_minutes, attribute_type, xp_reward, status, is_penalty, quest_kind, target_date)
VALUES
    (1,
     (SELECT id FROM goals WHERE user_id = 1 AND title = 'Correr uma meia maratona' LIMIT 1),
     'Corrida base controlada',
     'Primeiro passo do plano: construir base aerobica. Corrida continua, sem andar, mantendo o pace alvo.',
     '5 km a 6:00/km, concluir em ate 32 min',
     'STRAVA', 40, 'STR', 15, 'PENDING', FALSE, 'NORMAL', CURDATE());

-- 2) Arquitetura de Software / Front-end (STUDY -> resumo)
INSERT INTO quests
    (user_id, goal_id, title, description, metric_target, proof_type,
     estimated_minutes, attribute_type, xp_reward, status, is_penalty, quest_kind, target_date)
VALUES
    (1,
     (SELECT id FROM goals WHERE user_id = 1 AND title = 'Dominar Arquitetura de Software' LIMIT 1),
     'Monorepos: fundamentos',
     'Estudar o primeiro tema do backlog: arquitetura de monorepos (tooling, boundaries, versionamento).',
     'Estudar monorepos por 50 min e produzir um resumo com os conceitos-chave',
     'SUMMARY', 50, 'INT', 15, 'PENDING', FALSE, 'NORMAL', CURDATE());

-- 3) Fluencia tecnica em Ingles (READING -> foto do resumo)
INSERT INTO quests
    (user_id, goal_id, title, description, metric_target, proof_type,
     estimated_minutes, attribute_type, xp_reward, status, is_penalty, quest_kind, target_date)
VALUES
    (1,
     (SELECT id FROM goals WHERE user_id = 1 AND title = 'Fluencia tecnica em Ingles' LIMIT 1),
     'Artigo tecnico: distributed systems',
     'Ler um artigo tecnico avancado em ingles sobre sistemas distribuidos e resumir a mao.',
     'Ler 1 artigo (45 min) e escrever um resumo em ingles com os pontos principais',
     'PHOTO', 45, 'INT', 15, 'PENDING', FALSE, 'NORMAL', CURDATE());
