# Simulação — Especificação Técnica

---

## ⏱️ Estrutura da Partida

Uma partida é composta por **90 ticks**, onde cada tick representa 1 minuto de jogo.

```
match (90 ticks)
 ├─ 1º tempo: ticks 1–45
 ├─ intervalo: vestiário (escolha de carta) + ajustes táticos
 └─ 2º tempo: ticks 46–90
```

Cada tick executa o **loop de simulação** completo.

### Visualização na Demo
Sem gráfico de jogo. A partida é exibida estilo **SofaScore**:
- Campo esquemático com nota de cada jogador (atualizada por tick)
- Timeline lateral com eventos (gol, falta, cartão, substituição)
- Placar em tempo real
- O técnico pode fazer substituições e mudanças táticas durante a partida

### Visualização no Release — "Futebol de Botão"
Representação gráfica 2D top-down estilo futebol de botão:
- Campo verde visto de cima
- Jogadores representados como **discos/botões** com face anime e número
- Movimentação simplificada (deslizam pelo campo, não correm realisticamente)
- Animações de ação (chute, passe, desarme) como efeitos visuais sobre os discos
- A simulação tick a tick alimenta as posições e ações dos discos
- O técnico continua podendo pausar, substituir e ajustar tática em tempo real

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

> **Use sempre `RandomNumberGenerator` com seed fixa por partida.** Nunca use `GD.Randf()` global.

```csharp
// Inicialização da partida
var rng = new RandomNumberGenerator();
rng.Seed = matchSeed;  // seed derivada do matchId ou passada externamente

// Uso durante simulação
float rngValue = rng.RandfRange(-15.0f, 15.0f);
```

Isso garante que a mesma partida com a mesma seed produz sempre o mesmo resultado — essencial para testes e replay.

---

## 🧬 Atributos dos Jogadores

Escala: **0 – 100**

Dados base derivados de **scraping de dados reais** (TransferMarkt, FBref, etc.), mapeados para a escala do jogo. Nomes fictícios gerados algoritmicamente.

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

```csharp
if (trait.ContextMatches(currentSituation))
    successChance += trait.Bonus;
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

## 🏋️ Sistema de Treinamento

Inspirado em **Uma Musume**: alocação de jogadores em sessões + eventos aleatórios.

### Tipos de Sessão

| Tipo | Atributos afetados |
|---|---|
| `technical` | finishing, passing, dribbling, first_touch, technique |
| `tactical` | decisions, composure, positioning, anticipation, off_ball |
| `physical` | speed, acceleration, stamina, strength, agility |
| `mental` | consistency, leadership, morale, composure |

### Fórmula de Evolução

```
growth = base_growth_rate * session_intensity * age_factor * talent_modifier + event_bonus
```

| Componente | Descrição |
|---|---|
| `base_growth_rate` | Constante por atributo |
| `session_intensity` | Nível de intensidade escolhido pelo técnico |
| `age_factor` | Jovens (< 23) evoluem mais rápido; veteranos (> 30) mais devagar |
| `talent_modifier` | Potencial máximo do jogador afeta velocidade de ganho |
| `event_bonus` | Bônus de breakthrough ou inspiração (se ocorrer) |

### Eventos de Treino

Gerados por RNG seeded (assim como a simulação).

| Evento | Probabilidade base | Efeito |
|---|---|---|
| `breakthrough` | ~5% | Dobra o growth do atributo neste treino |
| `injury` | ~3% (maior em physical intenso) | Jogador fora por 3–14 dias |
| `conflict` | ~4% (maior se morale baixo) | -moral para jogadores envolvidos |
| `inspiration` | ~3% | Aprende trait ou melhora familiaridade posicional |
| `fatigue` | ~8% (maior sem descanso) | -stamina para próximo jogo |

---

## 🌍 Simulação Global (3 Níveis)

| Nível | Escopo | Método |
|---|---|---|
| 1 | Liga/time do jogador | Simulação tick a tick completa |
| 2 | Ligas importantes | Simulação resumida por evento |
| 3 | Resto do mundo | Resultado estatístico (Poisson) |

### Na Demo Pré-Alpha
Todos os 32 times são simulados em **Nível 1** (tick a tick). A simulação global com 3 níveis só é necessária quando o mundo expandir para ~195 países.

### Poisson para gols (Nível 3)

A distribuição de Poisson modela bem a marcação de gols em futebol:

```
P(k gols) = (λ^k * e^-λ) / k!
```

onde `λ` é a média de gols esperada, derivada dos ratings dos times.

### Promoção entre níveis (futuro)

- Fixo inicialmente (hardcoded por reputação da liga)
- Dinâmico no futuro (promove liga ao nível 2 quando o jogador tem interesse estratégico nela)

---

## 📊 Estado da Partida

Dados mantidos durante a simulação:

```csharp
public class MatchState
{
    public int ScoreHome { get; set; }
    public int ScoreAway { get; set; }
    public float PossessionHome { get; set; }  // 0.0–1.0
    public int CurrentTick { get; set; }
    public List<MatchEvent> Events { get; set; } = new();
    public Dictionary<int, float> PlayerRatings { get; set; } = new();  // playerId → rating
    public Dictionary<int, float> PlayerStamina { get; set; } = new();  // playerId → stamina (degrada)
}
```

---

## 🏟️ Formato de Competição na Demo

### Liga Nacional (por país)
- **8 times** por país
- **Eliminatória** (mata-mata direto)
  - Quartas: 4 jogos (8 → 4 times)
  - Semifinais: 2 jogos (4 → 2 times)
  - Final: 1 jogo
- Campeão classificado para o Mundial

### Mundial
- **4 campeões** nacionais
- Semifinais: 2 jogos
- Final: 1 jogo
- Vencedor → tela de vitória (convite para seleção)

---

## 🧪 Testabilidade

- Toda função de cálculo deve ser **pura** (sem side effects, sem acesso a nós de cena)
- RNG sempre injetado como parâmetro ou via dependência
- Estado da partida é um objeto de dados simples — pode ser serializado para testes

```csharp
// ✅ testável
public float CalculateSuccess(int attr, float chem, RandomNumberGenerator rng) { }

// ❌ não testável
public float CalculateSuccess()
{
    return GD.Randf() * someNode.Attribute;
}
```
