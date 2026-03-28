# CLAUDE.md — Instruções para Claude Code

## Projeto

**Eleven Legends** — Football Manager + Gacha em Godot 4 (C#/.NET 8.0).
Mundo fictício, nomes gerados algoritmicamente, estilo visual anime.

- **Engine:** Godot 4.4 com C# (.NET 8.0)
- **Plataformas alvo:** PC, Mobile (Android/iOS), Nintendo Switch, PS5
- **Banco de dados:** SQLite via Microsoft.Data.Sqlite
- **Testes:** xUnit (145 testes atualmente)

## Referências de UI/UX

A UI deve seguir o padrão **Duolingo** como referência principal:
- Cores vibrantes e brilhantes (paleta definida em `scenes/autoload/Theme.cs`)
- Botões 3D com sombra inferior que "afunda" ao pressionar
- Animações em tudo: entrada, hover, feedback de clique, celebrações
- Cards com efeito 3D hover (tilt + scale) via `HoverCard` component
- Drag-and-drop para cartas de jogadores (referência: Shopify Draggable)
- Efeito 3D hover nas cartas (referência: DaisyUI hover-3d)
- Transições suaves entre telas (nunca instantâneas)

Essas referências web são para o **tipo de interação desejado** — a implementação é em Godot nativo, NÃO em web/JS.

## Regras de Desenvolvimento

### Git

- **Commits atômicos** de responsabilidade única. Um commit = uma mudança lógica.
- **Nunca usar GPG** para assinar commits. Sempre usar `-c commit.gpgsign=false`.
- Mensagens em inglês, formato conventional commits (`feat:`, `fix:`, `refactor:`, etc.).
- Não fazer push sem pedir confirmação.

### Código

- Lógica de jogo fica em `src/`, nunca acoplada a cenas.
- UI/cenas ficam em `scenes/`. Cenas só orquestram e exibem.
- Componentes reutilizáveis de UI ficam em `scenes/components/`.
- Sempre tipos explícitos em C# (evitar `var` quando o tipo não é óbvio).
- RNG sempre injetado com seed (nunca `GD.Randf()` global).
- Strings visíveis ao jogador devem usar `Tr()` para i18n.
- Nomes de jogadores/times são sempre fictícios.

### UI

- Toda ação deve ter feedback visual (animações, easing).
- Nunca transições instantâneas — usar Tween com easing.
- Celebrar conquistas: gol, vitória, breakthrough.
- Cores semânticas: verde=sucesso, azul=info, amarelo=aviso, vermelho=perigo.
- Proibido: UI cinza/planilha, texto denso sem hierarquia, ações sem resposta visual.

## Estrutura

```
src/               # Lógica de jogo (simulação, competição, economia, transfers, persistence)
scenes/            # Cenas Godot (.tscn + .cs)
scenes/autoload/   # Singletons (SceneManager, Theme)
scenes/components/ # Componentes reutilizáveis (HoverCard, Anim)
tests/             # Testes xUnit
docs/              # Documentação de design
```

## Comandos Úteis

```bash
dotnet build                    # Compilar
dotnet test                     # Rodar testes
```
