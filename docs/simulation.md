# Simulação — Especificação Técnica

---

## ⏱️ Estrutura da Partida

Uma partida é composta por **90 ticks**, onde cada tick representa 1 minuto de jogo.

```
match (90 ticks)
 ├─ 1º tempo: ticks 1–45
 ├─ intervalo: vestiário (sistema de cartas)
 └─ 2º tempo: ticks 46–90
```

Cada tick executa o **loop de simulação** completo.

---

## 🔁 Loop de Simulação (por tick)

```
1. definir_posse()        → qual time controla a bola
2. escolher_acao()        → que ação o time com posse vai executar
3. selecionar_jogador()   → qual jogador executa a ação
4. calcular_sucesso()     → a ação foi bem-sucedida?
5. aplicar_resultado()    → atualiza estado da partida
6. gerar_evento()         → registra evento se relevante
```

---

## 🎯 Ações

| Ação | Atributo Principal | Condição |
|---|---|---|
| `pass` | `passing` | posse |
| `dribble` | `dribbling` | posse próximo a adversário |
| `shot` | `finishing` | zona de finalização |
| `cross` | `technique` | lateral com posse |
| `tackle` | `strength` | sem posse, próximo ao portador |
| `interception` | `anticipation` | sem posse, linha de passe |

---

## 🧮 Fórmula de Sucesso

```
success_chance = attribute + chemistry_bonus + morale_bonus + trait_bonus + rng_value
```

| Componente | Descrição | Escala |
|---|---|---|
| `attribute` | Atributo relevante para a ação | 0–100 |
| `chemistry_bonus` | Entrosamento com companheiros | 0–20 |
| `morale_bonus` | Estado moral do jogador | -10–+10 |
| `trait_bonus` | Bônus de trait contextual | 0–15 |
| `rng_value` | Aleatoriedade controlada | -15–+15 |

### RNG — Importante

> **Use sempre `RandomNumberGenerator` com seed fixa por partida.** Nunca use `randf()` global.

```gdscript
# Inicialização da partida
var rng := RandomNumberGenerator.new()
rng.seed = match_seed  # seed derivada do match_id ou passada externamente

# Uso durante simulação
var rng_value: float = rng.randf_range(-15.0, 15.0)
```

Isso garante que a mesma partida com a mesma seed produz sempre o mesmo resultado — essencial para testes e replay.

---

## 🧬 Atributos dos Jogadores

Escala: **0 – 100**

### Técnicos
| Atributo | Descrição |
|---|---|
| `finishing` | Eficiência nas finalizações |
| `passing` | Qualidade e precisão dos passes |
| `dribbling` | Capacidade de superar adversários |
| `first_touch` | Controle no primeiro toque |
| `technique` | Habilidade técnica geral |

### Mentais
| Atributo | Descrição |
|---|---|
| `decisions` | Qualidade das decisões sob pressão |
| `composure` | Tranquilidade em momentos críticos |
| `positioning` | Posicionamento tático (campo) |
| `anticipation` | Leitura antecipada das jogadas |
| `off_ball` | Movimentação sem a bola |

### Físicos
| Atributo | Descrição |
|---|---|
| `speed` | Velocidade máxima |
| `acceleration` | Capacidade de aceleração |
| `stamina` | Resistência física (degrada durante a partida) |
| `strength` | Força física em duelos |
| `agility` | Agilidade e mudança de direção |

### Especiais
| Atributo | Descrição |
|---|---|
| `consistency` | Regularidade de desempenho |
| `leadership` | Influência no vestiário e moral do time |
| `flair` | Tendência a jogadas improvisadas/criativas |
| `big_matches` | Performance amplificada em jogos decisivos |

### Goleiro (atributos exclusivos)
| Atributo | Descrição |
|---|---|
| `reflexes` | Reação a chutes |
| `handling` | Segurança nas defesas com as mãos |
| `gk_positioning` | Posicionamento do goleiro |
| `aerial` | Domínio aéreo (cruzamentos, escanteios) |

---

## ⭐ Traits (Habilidades Especiais)

### Filosofia
- Baseados em habilidades reais de jogadores
- Ativação **contextual** (só aplicam quando o contexto bate)
- Sem efeitos irreais ou fantasiosos

### Exemplos

| Trait | Contexto de ativação | Efeito |
|---|---|---|
| `Finesse Shot` | Chute dentro da área, ângulo fechado | +bonus em `finishing` |
| `Power Shot` | Chute com espaço e impulso | +force, -accuracy |
| `Close Control` | Drible em espaço reduzido | +bonus em `dribbling` |
| `Through Pass` | Passe em profundidade com espaço | +bonus em `passing` |
| `Interceptor` | Posição entre passador e receptor | +bonus em `interception` |
| `Aerial Dominance` | Disputa aérea (cabeceio, cruzamento) | +bonus em `aerial` |

### Implementação

```gdscript
if trait.context_matches(current_situation):
    success_chance += trait.bonus
```

---

## 🔗 Química

Bônus de entrosamento entre jogadores.

### Tipos de ligação

| Tipo | Critério |
|---|---|
| `club` | Jogadores do mesmo clube por N temporadas |
| `league` | Jogadores da mesma liga |
| `country` | Jogadores do mesmo país |
| `continent` | Jogadores do mesmo continente |

### Efeito

```
performance += chemistry_level * chemistry_factor
```

### Química por Proximidade (avançado)
- Jogadores em setores adjacentes do campo têm bônus de química ampliado
- Ex: lateral e ponta do mesmo país têm +quím por jogar próximos

---

## 📍 Posições e Penalidades

Cada jogador tem:
- **Posição primária** — sem penalidade
- **Posição secundária** — -10% de desempenho
- **Posição adaptada** — -20% de desempenho
- **Fora de posição** — -35% de desempenho

A familiaridade posicional **evolui com treino**, reduzindo progressivamente a penalidade até eliminar.

---

## 🌍 Simulação Global (3 Níveis)

| Nível | Escopo | Método |
|---|---|---|
| 1 | Liga/time do jogador | Simulação tick a tick completa |
| 2 | Ligas importantes | Simulação resumida por evento |
| 3 | Resto do mundo | Resultado estatístico (Poisson) |

### Poisson para gols (Nível 3)

A distribuição de Poisson modela bem a marcação de gols em futebol:

```
P(k gols) = (λ^k * e^-λ) / k!
```

onde `λ` é a média de gols esperada, derivada dos ratings dos times.

### Promoção entre níveis

- No MVP: fixo (hardcoded por reputação da liga)
- No futuro: dinâmico (promove liga ao nível 2 quando o jogador tem interesse estratégico nela)

---

## 📊 Estado da Partida

Dados mantidos durante a simulação:

```gdscript
class MatchState:
    var score_home: int
    var score_away: int
    var possession_home: float  # 0.0–1.0
    var current_tick: int
    var events: Array[MatchEvent]
    var player_ratings: Dictionary  # player_id → float
    var player_stamina: Dictionary  # player_id → float (degrada por tick)
```

---

## 🧪 Testabilidade

- Toda função de cálculo deve ser **pura** (sem side effects, sem acesso a nós de cena)
- RNG sempre injetado como parâmetro ou via dependência
- Estado da partida é um objeto de dados simples — pode ser serializado para testes

```gdscript
# ✅ testável
func calculate_success(attr: int, chem: float, rng: RandomNumberGenerator) -> float:

# ❌ não testável
func calculate_success() -> float:
    return randf() * some_node.attribute
```
