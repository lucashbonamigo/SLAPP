# Solo Leveling System — API (.NET 8)

MVP do "Sistema" de Solo Leveling. A API é o intermediário rigoroso; o frontend (Hostinger) só consome os endpoints. Um Worker (Motor do Sistema) roda às 21:30 para auditar as missões, acionar a IA (Gemini) e disparar o veredito por e-mail.

## Arquitetura

```
SoloLeveling.System.sln
└── src/
    ├── SoloLeveling.System.Core/     # Entidades, DbContext (MySQL), serviços (Gemini, SMTP, storage, leveling)
    ├── SoloLeveling.System.Api/      # ASP.NET Core Web API (JWT, Swagger) — consumida pelo frontend
    └── SoloLeveling.System.Worker/   # Worker Service agendado (21:30) — o Motor do Sistema
```

O banco MySQL **já existe** (script fornecido). O EF Core está em modo *database-first* (sem migrations): as entidades espelham exatamente as tabelas `users`, `quests`, `proofs`.

## Endpoints

| Método | Rota | Auth | Função |
|--------|------|------|--------|
| `POST` | `/api/v1/auth/login` | — | Autentica (e-mail + senha compartilhada) e devolve JWT |
| `GET`  | `/api/v1/player/status` | JWT | Ficha do personagem (level, XP, FOR/INT/SAB/VIT, deadline) |
| `GET`  | `/api/v1/quests/today` | JWT | Missões de hoje, indicando quais já têm evidência |
| `POST` | `/api/v1/quests/{id}/proof` | JWT | Upload (multipart) da evidência; **403 após o `daily_deadline`** |
| `GET`  | `/health` | — | Healthcheck |

### Login (MVP)
Enquanto não há hash de senha por usuário, o login valida o **e-mail cadastrado** + uma **senha compartilhada** (`ApiAuth:Password`). Troque por hash por usuário antes de produção séria.

## Como configurar

As credenciais ficam em `appsettings.json` (ou variáveis de ambiente / `.env` no Docker). Preencha:

- `ConnectionStrings:Default` — host/usuário/senha do MySQL
- `Jwt:SigningKey` — chave com no mínimo 32 caracteres
- `ApiAuth:Password` — senha do login
- `Gemini:ApiKey` — chave do Google AI Studio (usada no Worker)
- `Smtp:*` — credenciais de envio do veredito
- `Storage:ProofsPath` — `/var/www/system_proofs` na VPS

> Em desenvolvimento local, prefira **User Secrets** em vez de gravar segredos no `appsettings.json`.

## Rodar localmente

```powershell
# Restaurar + compilar
dotnet build SoloLeveling.System.sln

# API (Swagger em http://localhost:5080/swagger)
dotnet run --project src/SoloLeveling.System.Api

# Worker (Motor do Sistema)
dotnet run --project src/SoloLeveling.System.Worker
```

Fluxo de teste:
1. `POST /api/v1/auth/login` com o e-mail do jogador (ex: o cadastrado no INSERT) e a senha de `ApiAuth:Password`.
2. Copie o `accessToken` e autorize no Swagger (botão **Authorize**).
3. `GET /api/v1/player/status` e `GET /api/v1/quests/today`.
4. `POST /api/v1/quests/{id}/proof` enviando um arquivo de imagem.

## Rodar com Docker (VPS)

```bash
cp .env.example .env     # preencha os valores reais
docker compose up -d --build
```

Sobem dois containers isolados: `sololeveling-api` (porta 8080) e `sololeveling-worker`. As evidências ficam num volume compartilhado (`proofs` → `/var/www/system_proofs`), para o Worker ler o que a API gravou.

## O Motor do Sistema (Worker)

Executa diariamente às `Worker:RunAt` (padrão 21:30):
1. **Auditoria** — conta quests do dia sem evidência.
2. **Roteamento da IA**
   - *Cenário A* (tudo anexado): envia as imagens em Base64 ao Gemini → veredito de XP + metas de amanhã.
   - *Cenário B* (falta 1+): aciona o prompt de punição (XP negativo + Penalty Quest).
3. **Atualiza o banco** — status das quests (`COMPLETED`/`FAILED`), XP/nível do jogador, insere as missões de amanhã.
4. **Dispara o veredito** por SMTP.

## Notas de segurança (antes de produção)

- Trocar a senha compartilhada por hash por usuário (ex: ASP.NET Identity ou BCrypt).
- Garantir HTTPS no proxy reverso (Nginx/Caddy) à frente da API.
- Restringir CORS às origens reais do frontend.
- Nunca comitar segredos: use `.env` / User Secrets / variáveis de ambiente.
