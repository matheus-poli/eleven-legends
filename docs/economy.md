# Economia, Gacha e Transferências

---

## 💰 Duas Moedas

O jogo separa as finanças do clube das finanças pessoais do técnico.

| Moeda | Dono | Uso |
|---|---|---|
| `club_balance` | O clube | Transferências, salários de jogadores, infraestrutura |
| `manager_balance` | O técnico (jogador) | Salário pessoal, bônus de desempenho |

**Regra:** As duas moedas nunca se misturam. O técnico não tem acesso direto ao `club_balance`.

---

## 📥 Receitas do Clube

| Fonte | Descrição |
|---|---|
| Competições | Premiações por fase atingida (liga, copa, mundial) |
| Vendas de jogadores | Transferências saindo |
| Receita de base | Torcida, patrocínios (fixo por clube, varia com reputação) |
| **Bilheteria** | **Receita por jogo — inclui recompensas de Twitch/Kick** |

### Bilheteria e Twitch/Kick

A bilheteria é a principal ponte entre o jogo e a integração com streams:

```
ticket_revenue = base_attendance * ticket_price + twitch_rewards_value
```

| Componente | Descrição |
|---|---|
| `base_attendance` | Público base do clube (depende de reputação e resultados) |
| `ticket_price` | Preço do ingresso (ajustável pelo técnico) |
| `twitch_rewards_value` | Valor gerado por viewers que resgatam recompensas no stream |

Na demo: viewers de Twitch/Kick resgatam "ingressos virtuais" → geram `ticket_revenue` para o clube.

---

## 📤 Custos do Clube

| Tipo | Descrição |
|---|---|
| Salários | Folha salarial mensal dos jogadores |
| Compras | Transferências entrando |
| Crises | Multas, rescisões, eventos de narrativa |

## ☠️ Falência

```
if club_balance < limite_minimo:
    demissão_imediata()
```

O limite mínimo é configurável por clube (clubes menores têm tolerância menor).

---

## 💼 Finanças do Técnico

| Tipo | Descrição |
|---|---|
| Salário | Fixo por contrato, pago periodicamente |
| Bônus de desempenho | Títulos, classificações, vendas acima do valor |
| Multa rescisória | Técnico pode ser demitido com ou sem justa causa |

O `manager_balance` acumula entre empregos e serve como reserva de sobrevivência no desemprego.

---

## 🔄 Transferências

### Janelas

| Janela | Duração | Frequência |
|---|---|---|
| Principal | 20–30 dias | Anual (entre temporadas) |
| Especial | Variável | Antes de grandes torneios |

### Fora da janela

Movimentações permitidas fora de janela:
- Demissão de técnico
- Jogadores livres (sem contrato)
- Emergências médicas (lesões graves)

### Fluxo de transferência

```
Scout/Carta identifica jogador
 └─ Proposta do clube
      └─ Negociação (valor + salário)
           └─ Aceite / Recusa
                └─ Integração no elenco
```

### Na Demo
Transferências simplificadas: apenas jogadores livres e categoria de base (cartas de recrutamento). Negociação completa fica para versões posteriores.

---

## 🃏 Sistema de Cartas (Gacha)

Cartas não representam jogadores diretamente. Elas são **ferramentas táticas e de descoberta**.

### 1. Cartas de Vestiário (Intervalo)

Apresentadas durante o intervalo da partida. O técnico escolhe **1 de 3–5 cartas** reveladas aleatoriamente.

**Tipos de efeito:**

| Tipo | Exemplo de efeito |
|---|---|
| `moral_boost` | +moral para jogador específico ou time inteiro |
| `stamina_recovery` | Recupera stamina de jogadores com baixo rendimento |
| `tactic_buff` | Ativa pressão alta, linha mais avançada, etc. |
| `opponent_debuff` | Adversário começa 2º tempo com debuff de moral |
| `leadership_play` | Jogador com trait `leadership` inspira o grupo |

**Influências na geração de cartas:**
- Traits de liderança (mais cartas de `moral_boost`)
- Situação do jogo (perdendo → mais cartas de recuperação)
- Placar e contexto tático

> ⚠️ **Decisão pendente — consumíveis vs recorrentes:**
>
> **Opção A — Consumíveis:** Cada carta é um item. Usar gasta a carta. Cartas são obtidas via progressão, compra ou eventos. Cria economia de cartas e loop de gestão de recursos.
>
> **Opção B — Recorrentes:** Um pool de cartas disponíveis toda partida. Jogar mais partidas desbloqueia mais cartas no pool permanente. Mais simples, mais focado na tática.
>
> Esta decisão impacta diretamente o design da economia. **Não implementar lógica de consumo até a decisão ser tomada.**

### 2. Cartas de Recrutamento (Categoria de Base)

Revelam um jovem talento gerado proceduralmente.

- Atributos baseados em potencial (maioria oculto no início)
- Raridade influencia o potencial máximo do jogador
- O técnico decide se integra ao clube ou descarta
- Na integração Twitch/Kick: jovens da base podem ser vinculados a viewers do chat

### 3. Cartas de Olheiro

Representam uma recomendação de scout.

- Pode ser um **jogador existente** no mundo do jogo (com histórico real de stats)
- Pode ser um **jovem desconhecido** (sem histórico, alto risco/recompensa)
- O scout tem nível de confiabilidade que afeta a precisão da avaliação

### Visual de Cartas

Estilo **3D hover** (referência: DaisyUI hover-3d):
- Efeito de perspectiva 3D ao interagir (mouse hover / toque)
- Brilho e borda por raridade (bronze, prata, ouro, diamante)
- Stats principais visíveis na frente, stats detalhados no verso
- Implementado em Godot com shader de parallax, não web

---

## 📊 Balanço Econômico — Visão Geral

```
Receitas
  + Premiações de competições
  + Vendas de jogadores
  + Receita base do clube
  + Bilheteria (inclui recompensas Twitch/Kick)
  
Despesas
  - Folha salarial (mensal)
  - Contratações
  - Crises e multas

Resultado
  ± club_balance (acumula por temporada)

Se club_balance < limite → demissão imediata do técnico
```

---

## 🎯 Demo — Escopo Econômico

**Implementar na demo:**
- `club_balance` e `manager_balance` separados
- Receitas simples (premiação por resultado + receita base fixa + bilheteria)
- Bilheteria com integração Twitch/Kick (recompensas → ingressos → receita)
- Salários básicos (folha mensal)
- Falência → demissão → game over

**Adiar para fases posteriores:**
- Negociação de transferências completa
- Bônus de desempenho do técnico
- Sistema de cartas de olheiro
- Crises econômicas narrativas
- Preço de ingresso ajustável
