-- =============================================================================
-- Sistema v2 — Tabela de Objetivos (goals)
-- =============================================================================
-- A "memória de longo prazo" do Sistema. Cada objetivo é uma meta macro que a
-- IA fatia em tarefas diárias (quests), elevando a dificuldade gradualmente.
-- A API Gemini é stateless: é desta tabela que ela lê a direção e o histórico.
-- Rodar no banco u224972953_org1 (database-first, sem EF migrations).
-- =============================================================================

USE u224972953_org1;

CREATE TABLE IF NOT EXISTS goals (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,

    title VARCHAR(150) NOT NULL
        COMMENT 'Meta macro. Ex: "Correr uma meia maratona"',
    description TEXT
        COMMENT 'Contexto livre que o jogador fornece sobre o objetivo',

    -- A qual atributo da ficha este objetivo pertence (FOR/INT/SAB/VIT).
    attribute_type ENUM('STR','INT','WIS','VIT') NOT NULL
        COMMENT 'STR=FOR, INT=Inteligencia, WIS=SAB, VIT=Vitalidade',

    -- Tipo do objetivo: define como a IA gera a tarefa e qual comprovacao exigir.
    category ENUM('RUNNING','STUDY','READING','GENERIC') NOT NULL DEFAULT 'GENERIC'
        COMMENT 'RUNNING=corrida(Strava), STUDY=estudo(resumo/mapa), READING=leitura(foto resumo)',

    -- Meta mensuravel final do objetivo.
    target_metric VARCHAR(255)
        COMMENT 'Ex: "21.1 km em menos de 2h" | "Dominar o livro Clean Architecture"',

    -- Lista de conteudos/etapas que a IA vai consumindo dia a dia.
    -- Pode ser texto livre ou uma lista (ex: capitulos, distancias-alvo por semana).
    content_backlog TEXT
        COMMENT 'Backlog que a IA fatia em tarefas. Ex: lista de capitulos, plano de treino',

    status ENUM('ACTIVE','COMPLETED','PAUSED') NOT NULL DEFAULT 'ACTIVE'
        COMMENT 'Apenas objetivos ACTIVE geram tarefas',

    -- A IA atualiza isto a cada avaliacao: e a "memoria" do progresso.
    progress_notes TEXT
        COMMENT 'Atualizado pela IA a cada dia com a evolucao e o proximo passo',

    start_date DATE NOT NULL
        COMMENT 'Quando o objetivo comecou',
    target_date DATE NULL
        COMMENT 'Prazo final opcional do objetivo',

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_goals_user_status (user_id, status)
);

-- =============================================================================
-- Exemplos para alinharmos os objetivos (ajuste e descomente o que quiser usar).
-- Assumindo user_id = 1 (Jogador_01).
-- =============================================================================

-- INSERT INTO goals (user_id, title, attribute_type, category, target_metric, content_backlog, start_date, target_date)
-- VALUES
-- (1, 'Correr uma meia maratona',
--     'STR', 'RUNNING',
--     '21.1 km em menos de 2h',
--     'Plano progressivo: base 5km -> 8km -> 12km -> 16km -> 21km. Tiros de 400m e long runs no fim de semana.',
--     CURDATE(), DATE_ADD(CURDATE(), INTERVAL 90 DAY)),
--
-- (1, 'Dominar Arquitetura de Software',
--     'INT', 'STUDY',
--     'Ler e resumir "Clean Architecture" (Robert C. Martin)',
--     'Parte 1: Programming Paradigms; Parte 2: Design Principles (SOLID); Parte 3: Component Principles; Parte 4: Architecture; Parte 5: Details.',
--     CURDATE(), DATE_ADD(CURDATE(), INTERVAL 60 DAY)),
--
-- (1, 'Fluencia tecnica em Ingles',
--     'INT', 'READING',
--     'Ler 10 artigos tecnicos avancados em ingles e resumir cada um',
--     'Temas: distributed systems, databases internals, concurrency, networking. 1 artigo a cada 2-3 dias.',
--     CURDATE(), NULL);
