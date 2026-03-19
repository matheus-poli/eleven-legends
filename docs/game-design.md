# Game Design Document — Eleven Legends

> Documento vivo. Atualizar conforme decisões evoluem.

---

## 🧠 Visão Geral

**Eleven Legends** é um Football Manager com elementos de Gacha e integração com streams. O jogador assume o papel de um técnico de futebol e constrói sua carreira gerenciando clubes, negociando transferências, e tomando decisões táticas — com a possibilidade de seu público de stream influenciar o jogo.

---

## 🎯 Pilares

| # | Pilar | Descrição |
|---|---|---|
| 1 | Simulação realista | Partidas simuladas com atributos, traits e química |
| 2 | Narrativa emergente | Eventos, notícias e crises gerados pelo estado do jogo |
| 3 | Interação com chat | Votações e eventos via Twitch/Kick (futuro) |
| 4 | Progressão de carreira | Reputação, demissão, desemprego, game over |
| 5 | Risco econômico real | Falência e game over como consequências reais |

---

## ⚙️ Core Loop

```
Dia
 └─ Treino / Descanso / Gestão
      └─ Partida
           └─ Eventos durante a partida
                └─ Resultado final
                     └─ Economia (receitas, salários, prêmios)
                          └─ Notícias geradas
                               └─ Próximo dia
```

---

## 📅 Calendário

Tipos de dia:
- **Treino** — desenvolve atributos e familiaridade posicional
- **Descanso** — recupera stamina e moral
- **Jogo** — executa simulação de partida
- **Evento especial** — transferências, coletivas, crises

Ciclo anual:
- Liga nacional (temporada principal)
- Copa continental (para clubes qualificados)
- Copa mundial de clubes (anual, melhores clubes)
- Copa do Mundo de Clubes (a cada 4 anos, grupos + mata-mata)
- Copa do Mundo de Seleções (a cada 4 anos, eliminatórias)

---

## 🏆 Competições

### Clubes

| Competição | Formato | Frequência |
|---|---|---|
| Liga Nacional | Pontos corridos + playoff final | Anual |
| Copa Continental | Mata-mata, alta premiação | Anual |
| Mundial de Clubes | Curto, melhores clubes | Anual |
| Copa do Mundo de Clubes | Grupos + mata-mata, ranking global | A cada 4 anos |

### Seleções

- ~195 países participantes
- Eliminatórias regionais
- Copa do Mundo a cada 4 anos

---

## ⭐ Sistema de Nota (Rating)

| | |
|---|---|
| **Base** | 6.0 |
| **Escala** | 0.0 – 10.0 |

### Eventos Positivos

| Evento | Bônus |
|---|---|
| Gol | +1.5 |
| Assistência | +1.0 |
| Desarme | +0.3 |
| Drible bem-sucedido | +0.2 |

### Eventos Negativos

| Evento | Penalidade |
|---|---|
| Passe errado | -0.2 |
| Perda de bola | -0.3 |
| Falta cometida | -0.2 |

### Ajuste por Posição
- **Defensores:** valorizam desarmes e interceptações
- **Atacantes:** valorizam gols e assistências
- **Goleiros:** valorizam defesas e posicionamento

### Prêmios pós-partida
- **MVP** — jogador com maior nota
- **SVP** — segundo maior

---

## 🃏 Sistema de Cartas (Gacha)

### Contextos de uso

1. **Vestiário (intervalo):** o técnico escolhe 1 de 3–5 cartas reveladas; efeitos aplicados ao segundo tempo
2. **Recrutamento de base:** cartas revelam jovens talentos gerados proceduralmente
3. **Olheiros:** cartas trazem recomendações de jogadores (existentes ou jovens)

### Efeitos de cartas de vestiário
- Bônus de moral para grupo ou jogador específico
- Recuperação de stamina
- Buff tático (ex: pressão alta no 2º tempo)
- Debuff do adversário (ex: jogador adversário começa segundo tempo com moral reduzida)

> ⚠️ **Decisão pendente:** Cartas de vestiário são **consumíveis** (gastas ao usar) ou **recorrentes** (disponíveis todo intervalo)? Esta decisão impacta diretamente o design da economia.

---

## 🎮 Táticas

- Sistema livre: posições customizáveis, pressão, largura, tempo de posse
- Mudanças táticas em tempo real durante a partida
- Substituições (até o limite regulamentar)
- Famíliaridade tática evolui com treino

---

## 👔 Carreira do Técnico

### Estados

```
Empregado → Demitido → Desempregado → (Game Over se não encontrar clube)
```

### Causas de demissão
- Desempenho abaixo da expectativa do clube
- Desequilíbrio financeiro
- Conflitos de vestiário (eventos de narrativa)

### Reputação
- Escala 0–100
- Afeta quais propostas o técnico recebe
- Cresce com títulos, boas campanhas, vendas de jogadores
- Diminui com demissões, rebaixamentos, escândalos

### Desemprego
- Loop diário: aguardar propostas + aplicar em clubes abertos
- Propostas baseadas em reputação e histórico recente
- **Game Over:** se terminar a janela de transferências sem clube → fim de jogo
  > 🗣️ *Intencionalmente punitivo para criar stakes reais. Modo casual com prazo estendido pode ser adicionado futuramente.*

---

## 📰 Sistema de Notícias

Notícias são geradas automaticamente pelo estado do jogo.

### Tipos

| Tipo | Exemplo |
|---|---|
| `highlight` | "Jogador X brilhou com hat-trick" |
| `result` | "Time Y vence clássico por 3–1" |
| `crisis` | "Vestiário em crise após sequência de derrotas" |
| `streak` | "Time Z invicto há 8 jogos" |
| `transfer` | "Clube A vende jogador B por valor recorde" |

### Estrutura de dados

```json
{
  "type": "highlight",
  "player_id": 42,
  "text": "Jogador brilhou com hat-trick na vitória por 4–1",
  "match_id": 101,
  "date": 45
}
```

---

## 🎯 Escopo do MVP

**Incluído:**
- Simulação de partida (90 ticks)
- Atributos básicos
- Sistema de eventos e rating
- 1 liga, 2–4 times
- Loop econômico básico
- Carreira do técnico (estados empregado/demitido/desempregado)

**Explicitamente excluído do MVP:**
- Integração Twitch/Kick
- Transferências completas
- Mundo inteiro
- Seleções nacionais
- UI gráfica complexa
- Sistema de cartas completo
