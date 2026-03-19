# ⚽ Eleven Legends

> Em um mundo onde o futebol decidiu o destino das nações, apenas os melhores técnicos sobrevivem.

## 🌍 A Lore

No universo de **Eleven Legends**, o futebol se tornou a força mais poderosa da humanidade. Nações medem seu poder nos gramados. Títulos continentais valem mais que tratados. A Copa do Mundo é o evento supremo da civilização. Neste mundo, você é um técnico — e sua carreira determina o destino de clubes, torcidas e países inteiros.

---

## 🧠 Visão Geral

| | |
|---|---|
| **Gênero** | Football Manager + Gacha + Training Sim + Stream Integration |
| **Estilo visual** | Anime — representação fictícia da realidade |
| **Engine** | Godot 4 (C#) |
| **Banco de dados** | SQLite (via Microsoft.Data.Sqlite / nativo .NET) |
| **Arte** | Aseprite |
| **Backend futuro** | Node.js (bot de stream persistente) |

### Referências de Design

| Referência | O que pegamos dela |
|---|---|
| **PIX Football Manager / Brasfoot** | Gestão tática e econômica acessível (mas NÃO o estilo visual) |
| **Duolingo** | UI moderna, clean, colorida, com animações e feedback visual para tudo |
| **Uma Musume** | Sistema de treinamento com alocação + eventos aleatórios |
| **Cult of the Lamb** | Customização de aparência dos avatares de chat (Twitch/Kick) |
| **SofaScore** | Visualização de partida: ratings em tempo real, timeline de eventos |
| **Futebol de botão** | Representação gráfica da simulação no release (2D top-down, discos com faces anime) |

---

## 🎯 Pilares do Jogo

1. **Simulação realista** — partidas tick a tick com atributos, traits e química
2. **Treinamento divertido** — alocação de treino com eventos aleatórios (lesões, breakthroughs, conflitos)
3. **Narrativa emergente** — eventos, notícias e crises gerados pelo estado do jogo
4. **Interação com stream** — youth players como avatares do chat, bilheteria por recompensas
5. **Progressão de carreira** — reputação, demissão, desemprego, game over... ou glória
6. **Risco econômico real** — falência é game over; gestão inteligente é sobrevivência
7. **Mundo fictício baseado no real** — nomes algoritmicamente gerados, stats baseados em dados reais

---

## ⚙️ Core Loop

```
Dia → Treino (alocação + eventos) / Descanso / Gestão
    → Partida (eventos + ratings SofaScore)
    → Resultado → Economia (salários, bilheteria, prêmios)
    → Notícias → Próximo dia
```

---

## 🎮 Demo Pré-Alpha (Itch.io)

A primeira versão jogável com loop completo.

### Escopo

| Item | Valor |
|---|---|
| **Times** | 32 (4 países × 8 times cada) |
| **Competições** | Liga eliminatória por país → Mundial entre campeões |
| **Win condition** | Vencer liga → Classificar pro mundial → Vencer mundial → Convite para treinar a seleção |
| **Game over** | Falência do clube e/ou demissão do técnico |
| **Simulação** | Sem gráfico — eventos textuais + ratings em tempo real (estilo SofaScore) |
| **Dados** | Scrape manual único de bases reais → JSON com nomes fictícios |
| **Idiomas** | Português (BR) + Inglês |
| **Twitch/Kick** | Bilheteria por recompensas + youth players como avatares do chat |
| **Treinamento** | Alocação de jogadores em sessões + eventos aleatórios |
| **Distribuição** | Itch.io |

### O que NÃO está na demo

- Simulação gráfica "futebol de botão" (apenas na versão release)
- Transferências completas (negociação detalhada)
- Mundo inteiro (~195 países)
- Seleções jogáveis
- Co-op / multiplayer
- UI gráfica complexa

---

## 🔮 Visão de Futuro

| Área | Plano |
|---|---|
| **Plataformas** | Steam, Nintendo Switch, PS5, Mobile |
| **Simulação gráfica** | Estilo "futebol de botão" — 2D top-down com discos/sprites anime |
| **Multiplayer** | Co-op local, leaderboards online, possível modo online |
| **Stream** | Bot persistente Node.js, votações de chat, eventos ao vivo |
| **Idiomas** | Pesquisa dos idiomas mais usados em gaming → meta de cobertura máxima |
| **Mundo** | Todos os ~195 países com ligas, continentais, mundiais, seleções |

---

## 🗂️ Estrutura do Projeto

```
eleven-legends/
├── src/
│   ├── simulation/   # Motor de simulação (ticks, ações, fórmulas, treino)
│   ├── gacha/        # Sistema de cartas (vestiário, base, olheiros)
│   ├── manager/      # Gestão (time, elenco, tática, carreira)
│   ├── stream/       # Integração Twitch/Kick (avatares, bilheteria)
│   └── data/         # Modelos de dados, schemas, i18n
├── tools/            # Scripts externos (scraper de dados, gerador de nomes)
├── assets/           # Sprites, áudio, fontes (Aseprite)
├── scenes/           # Cenas Godot (.tscn)
├── tests/            # Testes unitários e integração
└── docs/             # Documentos de design e specs
    ├── game-design.md         # GDD completo
    ├── simulation.md          # Spec do motor de simulação
    ├── economy.md             # Economia, gacha, transferências
    ├── data-pipeline.md       # Scraper + gerador de nomes fictícios
    └── twitch-integration.md  # Twitch/Kick: avatares, bilheteria
```

---

## 📋 Roadmap

**Fase 1 — Motor de Simulação**
- [ ] Estrutura de 90 ticks por partida
- [ ] Atributos de jogadores (técnico, mental, físico, especial, goleiro)
- [ ] Fórmula de sucesso com seeded RNG
- [ ] Traits e química
- [ ] Rating pós-jogo (0–10, MVP/SVP)

**Fase 2 — Treinamento & Progressão**
- [ ] Sistema de alocação de treino (sessões por tipo)
- [ ] Eventos aleatórios de treino (lesões, breakthroughs, conflitos)
- [ ] Evolução de atributos via treino
- [ ] Familiaridade posicional

**Fase 3 — Loop de Jogo**
- [ ] Liga eliminatória (4 países × 8 times)
- [ ] Mundial entre campeões
- [ ] Calendário de temporada
- [ ] Economia básica (salários, bilheteria, prêmios)
- [ ] Carreira do técnico (reputação, demissão, win condition)

**Fase 4 — Gacha & Cartas**
- [ ] Cartas de vestiário (intervalo: buffs/debuffs)
- [ ] Cartas de categoria de base (recrutamento)
- [ ] Visual de carta 3D (ref: DaisyUI hover-3d)

**Fase 5 — Twitch/Kick Integration**
- [ ] Youth players como avatares do chat
- [ ] Customização de aparência (ref: Cult of the Lamb)
- [ ] Bilheteria via recompensas resgatadas
- [ ] Bot persistente Node.js

**Fase 6 — Data Pipeline**
- [ ] Scraper de bases de futebol (TransferMarkt, FBref, etc.)
- [ ] Algoritmo de nomes fictícios (anagramas/sílabas)
- [ ] Pipeline: scrape → normalização → JSON
- [ ] Atualização periódica de dados

**Fase 7 — Mundo Expandido**
- [ ] Todos os ~195 países com ligas nacionais
- [ ] Competições continentais e mundiais
- [ ] Seleções nacionais e Copa do Mundo
- [ ] Simulação global 3 níveis (Poisson)

**Fase 8 — Multiplataforma & Online**
- [ ] Steam, Switch, PS5, Mobile
- [ ] Co-op local
- [ ] Leaderboards online
- [ ] Internacionalização (meta: máximo de idiomas possível)

---

## 🚀 Como Começar

1. Instale o [Godot 4](https://godotengine.org/download)
2. Clone este repositório
3. Abra `project.godot` no Godot 4
4. Rode o projeto (`F5`)

---

## 📚 Documentação

- [`docs/game-design.md`](docs/game-design.md) — GDD completo
- [`docs/simulation.md`](docs/simulation.md) — Spec técnica do motor de simulação
- [`docs/economy.md`](docs/economy.md) — Economia, gacha e transferências
- [`docs/data-pipeline.md`](docs/data-pipeline.md) — Scraper de dados + nomes fictícios
- [`docs/twitch-integration.md`](docs/twitch-integration.md) — Integração Twitch/Kick
- [`AGENTS.md`](AGENTS.md) — Guia para agentes de IA

---

## 📄 Licença

TBD
