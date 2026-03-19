# ⚽ Eleven Legends

> Football Manager + Gacha + Stream Integration

## 🧠 Visão Geral

| | |
|---|---|
| **Gênero** | Football Manager + Gacha + Stream Integration |
| **Plataforma** | PC (Steam + Itch.io) |
| **Engine** | Godot 4 |
| **Foco inicial** | Simulação de partidas (sem UI gráfica complexa) |
| **Banco de dados** | SQLite (via plugin godot-sqlite) |
| **Backend futuro** | Node.js (bot de stream persistente) |

---

## 🎯 Pilares do Jogo

1. **Simulação realista de futebol** — partidas simuladas tick a tick (90 ticks = 90 minutos)
2. **Narrativa emergente** — eventos, notícias e crises gerados pelo estado do jogo
3. **Interação com chat** — votações e eventos disparados pelo público (futuro)
4. **Progressão de carreira do técnico** — reputação, demissão, desemprego, game over
5. **Gestão econômica com risco real** — falência e demissão como consequências reais

---

## ⚙️ Core Loop

```
Dia → Treino/Descanso/Gestão → Partida → Eventos → Resultado → Economia → Notícias → Próximo dia
```

---

## 🎮 MVP (Primeira Versão)

O MVP foca em fazer o loop central funcionar antes de qualquer expansão.

**Incluído:**
- Motor de simulação de partidas (90 ticks)
- Atributos básicos de jogadores
- Sistema de eventos e rating pós-jogo
- 1 liga, 2–4 times
- Loop econômico básico

**Não incluído ainda:**
- Integração com Twitch/Kick
- Transferências completas
- Mundo inteiro (~195 países)
- Seleções nacionais
- UI gráfica complexa

---

## 🗂️ Estrutura do Projeto

```
eleven-legends/
├── src/
│   ├── simulation/   # Motor de simulação de partidas (ticks, ações, fórmulas)
│   ├── gacha/        # Sistema de cartas (vestiário, recrutamento base, olheiros)
│   ├── manager/      # Gestão de time, elenco, tática, carreira do técnico
│   └── stream/       # Integração com Twitch/Kick (futuro)
├── assets/           # Sprites, áudio, fontes (Aseprite)
├── scenes/           # Cenas Godot (.tscn)
├── tests/            # Testes unitários e de integração
└── docs/             # Documentos de design e especificações técnicas
    ├── game-design.md   # GDD completo
    ├── simulation.md    # Spec técnica da simulação
    └── economy.md       # Economia, gacha, transferências
```

---

## 📋 Roadmap

**Fase 1 — Motor de Simulação**
- [ ] Estrutura de 90 ticks por partida
- [ ] Atributos de jogadores (técnico, mental, físico, especial)
- [ ] Fórmula de sucesso com seeded RNG
- [ ] Sistema de traits e química
- [ ] Rating pós-jogo (0–10, MVP/SVP)

**Fase 2 — Loop de Jogo**
- [ ] 1 liga com pontos corridos
- [ ] Calendário de temporada (treino, descanso, jogos)
- [ ] Sistema de notícias e eventos
- [ ] Economia básica (receitas, salários)
- [ ] Carreira do técnico (reputação, demissão)

**Fase 3 — Gacha & Progressão**
- [ ] Cartas de vestiário (buffs/debuffs no intervalo)
- [ ] Cartas de recrutamento (categoria de base + olheiros)
- [ ] Sistema de transferências (janelas)

**Fase 4 — Mundo Expandido**
- [ ] Simulação global 3 níveis (Poisson)
- [ ] Competições continentais e mundiais
- [ ] Seleções nacionais

**Fase 5 — Stream Integration**
- [ ] Integração Twitch/Kick (votações, eventos de chat)
- [ ] Bot persistente via Node.js

---

## 🚀 Como Começar

1. Instale o [Godot 4](https://godotengine.org/download)
2. Clone este repositório
3. Abra `project.godot` no Godot 4
4. Rode o projeto (`F5`)

---

## 📚 Documentação

- [`docs/game-design.md`](docs/game-design.md) — Design do jogo completo (GDD)
- [`docs/simulation.md`](docs/simulation.md) — Spec técnica do motor de simulação
- [`docs/economy.md`](docs/economy.md) — Economia, gacha e transferências
- [`AGENTS.md`](AGENTS.md) — Guia para agentes de IA e colaboradores técnicos

---

## 📄 Licença

TBD
