# AGENTS.md — Guia para Agentes de IA

Este arquivo orienta agentes de IA (GitHub Copilot, Cursor, Claude, etc.) sobre como trabalhar neste repositório de forma consistente com as decisões de arquitetura e design do projeto.

---

## 🧠 O que é este projeto?

**Eleven Legends** é um jogo de Football Manager com elementos de Gacha e integração com streams ao vivo.

- **Engine:** Godot 4 (GDScript)
- **Plataforma alvo:** PC (Steam + Itch.io)
- **Banco de dados:** SQLite via plugin [godot-sqlite](https://github.com/2shady4u/godot-sqlite)
- **Backend futuro:** Node.js (apenas para bot de stream persistente)
- **Arte:** Aseprite

O foco inicial é **simulação de partidas sem UI gráfica** — toda lógica de jogo deve funcionar e ser testável sem depender de cenas ou nós visuais.

---

## 🗂️ Estrutura de Pastas

```
src/
├── simulation/   # Motor de simulação — tick engine, ações, fórmulas, traits
├── gacha/        # Sistema de cartas — vestiário, recrutamento, olheiros
├── manager/      # Gestão — time, elenco, tática, carreira do técnico
└── stream/       # Integração com Twitch/Kick (futuro, não implementar agora)

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

Não misture lógica de nível 1 com os outros níveis. Cada nível tem seu próprio módulo.

---

## 🃏 Sistema de Cartas (Gacha)

Cartas são usadas em três contextos:
1. **Vestiário (intervalo):** jogador escolhe 1 de 3–5 cartas; efeitos de moral/stamina/buffs para o segundo tempo
2. **Recrutamento de base:** cartas revelam jovens talentos da categoria de base
3. **Olheiros:** cartas de recomendação (podem ser jogadores existentes ou novos)

> ⚠️ **A definir:** cartas de vestiário são consumíveis (gastas ao usar) ou recorrentes? Não implemente lógica de consumo ainda — deixe essa decisão em aberto.

---

## 💰 Economia

Duas moedas separadas:
- `club_balance` — dinheiro do clube (transferências, salários, receitas)
- `manager_balance` — dinheiro pessoal do técnico (salário, bônus de desempenho)

Nunca misture as duas. O técnico não pode usar `club_balance` para fins pessoais.

---

## 🔴 O que NÃO fazer

- **Não acople lógica de simulação a nós de cena** (`Node`, `Node2D`, etc.)
- **Não use `randf()` global** — sempre use RNG injetado com seed
- **Não implemente stream integration agora** — a pasta `stream/` existe mas é futura
- **Não crie tabelas SQLite sem documentar o schema em `docs/`**
- **Não misture `club_balance` e `manager_balance`**
- **Não simule o mundo inteiro no MVP** — comece com 1 liga, 2–4 times

---

## ✅ Checklist antes de submeter código

- [ ] Tipos estáticos em todas as funções públicas?
- [ ] Lógica de jogo em `src/`, não em cena?
- [ ] RNG injetado (não global)?
- [ ] Há testes para a lógica nova?
- [ ] O schema do banco está documentado se adicionou tabelas?
