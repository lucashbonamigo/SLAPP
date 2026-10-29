# Solo Leveling — Frontend (React + Vite)

Front simples que consome a API do "Sistema". Tela de login (JWT) e dashboard com a ficha do personagem, as missões do dia e upload de evidência.

## Stack
- React 18 + TypeScript
- Vite (dev server + build)
- `fetch` nativo, sem libs extras

## Estrutura
```
src/
  api.ts              # cliente HTTP + token (localStorage)
  types.ts            # tipos espelhando as respostas da API
  App.tsx             # alterna Login / Dashboard conforme o token
  components/
    Login.tsx         # POST /api/v1/auth/login
    Dashboard.tsx     # carrega status + quests
    PlayerCard.tsx    # ficha: level, XP, FOR/INT/SAB/VIT, deadline
    QuestCard.tsx     # missão + upload de evidência (multipart)
  styles.css          # tema "Sistema" (dark/neon)
```

## Rodar em dev
```powershell
npm install
npm run dev
```
Abre em `http://localhost:5173`. O Vite faz **proxy de `/api` para `http://localhost:5080`** (a API), então não precisa mexer em CORS durante o desenvolvimento. Suba a API antes (`dotnet run --project ../Backend/src/SoloLeveling.System.Api`).

## Build de produção
```powershell
npm run build      # gera dist/
npm run preview    # serve o dist/ localmente para conferir
```

Para publicar na Hostinger, suba o conteúdo de `dist/`. Em produção, defina a URL da API num arquivo `.env`:
```
VITE_API_BASE_URL=https://api.seudominio.com
```
(Deixe vazio em dev para usar o proxy.)

## Login
Use o **e-mail cadastrado** na tabela `users` e a senha compartilhada do backend (`ApiAuth:Password` do `appsettings.json`).
