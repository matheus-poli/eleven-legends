# AGENTS.md — Guia para Agentes de IA

Este arquivo orienta agentes de IA (GitHub Copilot, Cursor, Claude, etc.) sobre como trabalhar neste repositório de forma consistente com as decisões de arquitetura e design do projeto.

---

## 🧠 O que é este projeto?

**Eleven Legends** é um jogo de Football Manager com elementos de Gacha, treinamento estilo Uma Musume e integração com streams ao vivo. O mundo do jogo é fictício mas baseado na realidade — jogadores e times têm nomes gerados algoritmicamente a partir de dados reais, com estilo visual anime.

### Lore

> No universo de Eleven Legends, o futebol se tornou a força mais poderosa da humanidade. Nações medem seu poder nos gramados. O jogador é um técnico cuja carreira determina o destino de clubes e países.

- **Engine:** Godot 4 (GDScript)
- **Plataforma alvo:** PC (Itch.io para demo, Steam para release)
- **Banco de dados:** SQLite via plugin [godot-sqlite](https://github.com/2shady4u/godot-sqlite)
- **Backend futuro:** Node.js (bot de stream persistente)
- **Arte:** Aseprite (estilo anime)
- **Dados:** Baseados em scrape de dados reais (TransferMarkt, FBref, etc.)

O foco inicial é **simulação de partidas sem UI gráfica de jogo** — toda lógica deve funcionar e ser testável sem depender de cenas ou nós visuais. A visualização de partida na demo é estilo SofaScore (eventos + ratings em tempo real).

---

## 🗂️ Estrutura de Pastas

```
src/
├── simulation/   # Motor de simulação — tick engine, ações, fórmulas, traits
├── gacha/        # Sistema de cartas — vestiário, recrutamento, olheiros
├── manager/      # Gestão — time, elenco, tática, treino, carreira do técnico
├── stream/       # Integração Twitch/Kick — avatares, bilheteria
└── data/         # Modelos de dados, schemas, i18n

tools/            # Scripts externos — scraper de dados, gerador de nomes fictícios
docs/             # Documentação de design (não é código)
tests/            # Testes unitários (GUT framework ou scripts nativos)
scenes/           # Cenas Godot (.tscn) — apenas UI e apresentação
assets/           # Recursos visuais e de áudio
```

**Regra:** lógica de jogo fica em `src/`, nunca acoplada a nós de cena. Cenas só orquestram e exibem — não calculam.

---

## ⚙️ Convenções de Código (GDScript)

### Nomenclatura
- `snake_case` para variáveis, funções e arquivos
- `PascalCase` para nomes de classe (`class_name`)
- Constantes em `UPPER_SNAKE_CASE`
- Prefixo `_` para funções e variáveis privadas

### Tipagem
- **Sempre use tipagem estática.** GDScript 4 suporta tipos e isso melhora performance e autocomplete.

```gdscript
# ✅ correto
var speed: float = 0.0
func calculate_success(attribute: int, chemistry: float) -> float:

# ❌ evitar
var speed = 0.0
func calculate_success(attribute, chemistry):
```

### Structs / Value Objects
- Use `class` internas ou `Resource` para representar dados de domínio (jogador, partida, evento).
- Prefira `Resource` quando o dado precisar ser salvo/carregado.

### Internacionalização (i18n)
- **Todas as strings visíveis ao jogador devem usar `tr()`** desde o início.
- Chaves de tradução em `UPPER_SNAKE_CASE`: `tr("MATCH_GOAL_SCORED")`
- Idiomas da demo: PT-BR + EN
- Nunca hardcode texto visível ao jogador diretamente no código.

---

## ⚽ Domínio do Jogo — Termos-Chave

Estes são os conceitos centrais. Use estes nomes exatos no código:

| Termo | Descrição |
|---|---|
| `tick` | 1 unidade de tempo de jogo = 1 minuto de partida (90 por jogo) |
| `match` | Uma partida completa (90 ticks) |
| `player` | Um jogador do elenco (não o usuário) |
| `manager` / `coach` | O técnico controlado pelo usuário |
| `attribute` | Estatística individual de um jogador (0–100) |
| `trait` | Habilidade especial contextual de um jogador |
| `chemistry` | Bônus de entrosamento (clube, liga, país, continente) |
| `morale` | Estado emocional do jogador (afeta performance) |
| `rating` | Nota do jogador na partida (0.0–10.0, base 6.0) |
| `event` | Acontecimento gerado durante simulação (gol, falta, etc.) |
| `card` | Carta do sistema gacha (vestiário, recrutamento) |
| `reputation` | Reputação do técnico (0–100, afeta propostas de emprego) |
| `training_session` | Sessão de treino com tipo e jogadores alocados |
| `training_event` | Evento aleatório durante treino (lesão, breakthrough, conflito) |
| `ticket_revenue` | Receita de bilheteria (inclui recompensas Twitch/Kick) |
| `youth_avatar` | Jogador de base vinculado a um espectador do chat |

---

## 🧮 Arquitetura da Simulação

A simulação roda em **batch** (sem esperar UI). A cada tick:

1. Definir posse de bola
2. Escolher ação (`pass`, `dribble`, `shot`, `cross`, `tackle`, `interception`)
3. Selecionar jogador executor
4. Calcular sucesso via fórmula
5. Aplicar resultado ao estado da partida
6. Gerar evento se relevante

### Fórmula de Sucesso

```
success = attribute + chemistry_bonus + morale_bonus + trait_bonus + rng
```

- `rng` é gerado por um `RandomNumberGenerator` com **seed fixa por partida** (reproduzível)
- Nunca use `randf()` global — sempre passe o RNG como dependência
- Isso permite testes determinísticos

### Simulação Global (3 Níveis)

- **Nível 1:** Partida do jogador — simulação tick a tick completa
- **Nível 2:** Ligas importantes — simulação resumida por evento
- **Nível 3:** Resto do mundo — resultado estatístico via distribuição de Poisson

Na demo pré-alpha, tudo é **Nível 1** (apenas 32 times).

---

## 🏋️ Sistema de Treinamento

Inspirado em **Uma Musume**: alocação de jogadores em sessões de treino + eventos aleatórios.

- Cada sessão de treino tem um tipo (técnico, tático, físico, mental)
- O técnico aloca jogadores nas sessões disponíveis
- Durante o treino, eventos aleatórios podem acontecer:
  - **Breakthrough:** jogador evolui mais rápido
  - **Lesão:** jogador se machuca e fica fora por N dias
  - **Conflito:** jogadores com química ruim brigam, moral cai
  - **Inspiração:** jogador aprende novo trait ou melhora familiaridade posicional

---

## 🃏 Sistema de Cartas (Gacha)

Cartas são usadas em três contextos:
1. **Vestiário (intervalo):** técnico escolhe 1 de 3–5 cartas; efeitos de moral/stamina/buffs para o segundo tempo
2. **Recrutamento de base:** cartas revelam jovens talentos da categoria de base
3. **Olheiros:** cartas de recomendação (podem ser jogadores existentes ou novos)

As cartas de jogador têm **visual estilo 3D hover** (referência: DaisyUI hover-3d), mas implementado em Godot.

> ⚠️ **A definir:** cartas de vestiário são consumíveis (gastas ao usar) ou recorrentes? Não implemente lógica de consumo ainda — deixe essa decisão em aberto.

---

## 🎮 Integração Twitch/Kick

- **Youth players como avatares do chat:** espectadores do stream são vinculados a jogadores de base com aparência customizável (referência: Cult of the Lamb Twitch extension)
- **Bilheteria por recompensas:** viewers resgatam recompensas → geram receita de bilheteria para o clube
- Na demo, implementar versão básica (avatares + bilheteria)
- Futuro: votações, eventos de chat, bot persistente Node.js

---

## 🌐 Nomes Fictícios

O jogo usa **nomes fictícios gerados algoritmicamente** a partir de dados reais:
- Algoritmo: anagramas e sílabas embaralhadas dos nomes reais
- Nomes de times, jogadores, ligas e países fictícios referenciam os reais
- Os dados base vêm de scraping de TransferMarkt, FBref, etc.
- O gerador de nomes fica em `tools/` (script externo, não GDScript)

---

## 💰 Economia

Duas moedas separadas:
- `club_balance` — dinheiro do clube (transferências, salários, receitas, bilheteria)
- `manager_balance` — dinheiro pessoal do técnico (salário, bônus de desempenho)

Nunca misture as duas. O técnico não pode usar `club_balance` para fins pessoais.

---

## 🔴 O que NÃO fazer

- **Não acople lógica de simulação a nós de cena** (`Node`, `Node2D`, etc.)
- **Não use `randf()` global** — sempre use RNG injetado com seed
- **Não crie tabelas SQLite sem documentar o schema em `docs/`**
- **Não misture `club_balance` e `manager_balance`**
- **Não hardcode strings visíveis ao jogador** — use `tr()` sempre
- **Não use nomes reais de jogadores/times** — sempre fictícios
- **Não implemente mais de 32 times na demo** — escalar depois

---

## ✅ Checklist antes de submeter código

- [ ] Tipos estáticos em todas as funções públicas?
- [ ] Lógica de jogo em `src/`, não em cena?
- [ ] RNG injetado (não global)?
- [ ] Strings visíveis ao jogador usando `tr()`?
- [ ] Há testes para a lógica nova?
- [ ] O schema do banco está documentado se adicionou tabelas?
- [ ] Nomes de jogadores/times são fictícios (não reais)?
