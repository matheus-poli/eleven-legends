# CLAUDE.md — Instruções para Claude Code

## Projeto

**Eleven Legends** — Football Manager web game.
Mundo fictício com times fictícios, países reais, estilo visual inspirado no Duolingo.

- **Stack:** Next.js 15 (App Router) + TypeScript + TailwindCSS 4 + DaisyUI
- **Animações:** anime.js
- **Drag & Drop:** dnd-kit
- **Icons:** heroicons, flag-icons, custom SVGs
- **State:** Zustand
- **RNG:** seedrandom (determinístico)
- **Persistence:** IndexedDB via idb
- **Testes:** Vitest
- **Package Manager:** pnpm

## Referências de UI/UX

A UI deve seguir o padrão **Duolingo** como referência principal:
- Cores vibrantes e brilhantes (paleta DaisyUI customizada)
- Botões 3D com sombra inferior (DaisyUI btn classes)
- Animações em tudo: entrada, hover, feedback de clique, celebrações (anime.js)
- Cards com efeito 3D hover (DaisyUI hover-3d)
- Drag-and-drop para cartas de jogadores (dnd-kit)
- Transições suaves entre telas (nunca instantâneas)
- Ícones: heroicons + flag-icons + SVGs customizados
- Logos de clubes: assets customizados

## Regras de Desenvolvimento

### Git
- Commits atômicos em inglês, conventional commits (`feat:`, `fix:`, etc.)
- Sempre push direto para main.
- Nunca usar GPG para assinar commits: `-c commit.gpgsign=false`

### Código
- Game engine em `engine/` — lógica pura TypeScript, sem dependência de React/Next/DOM
- UI em `app/` e `components/`
- Componentes reutilizáveis em `components/ui/`
- State management com Zustand em `store/`
- RNG sempre injetado com seed (nunca Math.random() global)
- Países são reais (Brasil, España, England, Italia). Times são fictícios.

### UI
- Toda ação deve ter feedback visual (animações, easing)
- Nunca transições instantâneas
- Cores semânticas: verde=sucesso, azul=info, amarelo=aviso, vermelho=perigo
- Proibido: UI cinza/planilha, texto denso sem hierarquia, ações sem resposta visual

## Estrutura

```
app/                  # Next.js pages (App Router)
components/           # React components (ui, pitch, match, training)
engine/               # Game logic (models, enums, simulation, competition, economy, transfers)
store/                # Zustand stores
lib/                  # Utilities
public/               # Static assets (icons, flags)
__tests__/            # Vitest tests
docs/                 # Design docs
```

## Comandos

```bash
pnpm dev              # Dev server
pnpm build            # Production build
pnpm test             # Run tests
pnpm lint             # Lint
```
