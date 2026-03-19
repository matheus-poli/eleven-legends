# Game Design Document — Eleven Legends

> Documento vivo. Atualizar conforme decisões evoluem.

---

## 🌍 Lore

No universo de **Eleven Legends**, o futebol se tornou a força mais poderosa da humanidade. Guerras foram substituídas por partidas. Tratados internacionais são negociados nos vestiários. A Copa do Mundo é o evento supremo da civilização — e vencer uma liga nacional é mais relevante do que qualquer eleição.

Neste mundo, você é um **técnico de futebol**. Sua carreira começa em um clube e pode terminar liderando uma seleção nacional — ou na ruína do desemprego. Cada decisão importa: treinar o jogador certo, escalar a formação perfeita, sobreviver à pressão da torcida e administrar as finanças de um clube que vive à beira do colapso.

O mundo do jogo é **fictício mas baseado na realidade**: todos os jogadores, times, ligas e países são versões fictícias de seus equivalentes reais, com nomes gerados algoritmicamente (anagramas e sílabas embaralhadas) e stats derivados de dados reais.

---

## 🧠 Visão Geral

| | |
|---|---|
| **Gênero** | Football Manager + Gacha + Training Sim + Stream Integration |
| **Estilo visual** | Anime — representação fictícia da realidade |
| **Engine** | Godot 4 (C#) |
| **Plataforma inicial** | PC (Itch.io para demo, Steam para release) |
| **Futuro** | Steam, Switch, PS5, Mobile, Co-op local, Online |
| **Simulação gráfica** | Release: estilo "futebol de botão" (2D top-down, discos com faces anime) |

### Referências de Design

| Referência | Influência |
|---|---|
| **PIX Football Manager / Brasfoot** | Gestão tática e econômica acessível |
| **Uma Musume** | Treinamento: alocação de sessões + eventos aleatórios |
| **Cult of the Lamb** | Customização de aparência dos avatares de chat |
| **SofaScore** | Visualização de partida: ratings em tempo real, timeline de eventos |
| **DaisyUI hover-3d** | Estilo visual das cartas de jogadores |
| **Futebol de botão** | Representação gráfica da simulação no release (2D top-down, discos anime) |

---

## 🎯 Pilares

| # | Pilar | Descrição |
|---|---|---|
| 1 | Simulação realista | Partidas simuladas tick a tick com atributos, traits e química |
| 2 | Treinamento divertido | Alocação de treino + eventos aleatórios (lesões, breakthroughs, conflitos) |
| 3 | Narrativa emergente | Eventos, notícias e crises gerados pelo estado do jogo |
| 4 | Interação com stream | Youth players como avatares, bilheteria por recompensas |
| 5 | Progressão de carreira | Reputação, demissão, desemprego, game over... ou glória |
| 6 | Risco econômico real | Falência e game over como consequências reais |
| 7 | Mundo fictício/real | Nomes algoritímicos, stats baseados em dados reais |

---

## ⚙️ Core Loop

```
Dia
 └─ Treino (alocação de sessões + eventos aleatórios)
      / Descanso (recupera stamina e moral)
      / Gestão (táticas, transferências, vestiário)
 └─ Partida (eventos textuais + ratings estilo SofaScore)
      └─ Intervalo: escolha de carta de vestiário
      └─ Resultado final
 └─ Economia (salários, bilheteria, prêmios, recompensas Twitch)
 └─ Notícias geradas
 └─ Próximo dia
```

---

## 📅 Calendário

### Tipos de Dia
- **Treino** — aloca jogadores em sessões (técnico, tático, físico, mental); eventos aleatórios
- **Descanso** — recupera stamina e moral
- **Jogo** — simulação de partida
- **Evento especial** — transferências, coletivas, crises

### Ciclo Anual (versão completa)
- Liga nacional (temporada principal)
- Copa continental (clubes qualificados)
- Mundial de clubes (anual, melhores clubes)
- Copa do Mundo de Clubes (a cada 4 anos, grupos + mata-mata)
- Copa do Mundo de Seleções (a cada 4 anos, eliminatórias)

### Ciclo da Demo
- Liga eliminatória (por país, rodadas curtas)
- Mundial entre os campeões de cada país

---

## 🏆 Competições

### Versão Completa — Clubes

| Competição | Formato | Frequência |
|---|---|---|
| Liga Nacional | Pontos corridos + playoff final | Anual |
| Copa Continental | Mata-mata, alta premiação | Anual |
| Mundial de Clubes | Curto, melhores clubes | Anual |
| Copa do Mundo de Clubes | Grupos + mata-mata, ranking global | A cada 4 anos |

### Versão Completa — Seleções

- ~195 países participantes (todos fictícios baseados nos reais)
- Eliminatórias regionais por continente
- Copa do Mundo a cada 4 anos

### Demo Pré-Alpha

| Competição | Formato |
|---|---|
| Liga Nacional (4 países) | Eliminatória (mata-mata curto, 8 times por país) |
| Mundial | Torneio entre os 4 campeões nacionais |

---

## 🏋️ Sistema de Treinamento

Inspirado em **Uma Musume**: o treinamento é uma atividade divertida, não só um slider de XP.

### Mecânica

1. O técnico vê as **sessões de treino disponíveis** para o dia
2. Cada sessão tem um **tipo** (técnico, tático, físico, mental) e slots limitados
3. O técnico **aloca jogadores** nas sessões
4. O treino roda e **eventos aleatórios** podem acontecer

### Eventos de Treino

| Tipo | Efeito |
|---|---|
| `breakthrough` | Jogador evolui atributo mais rápido que o normal |
| `injury` | Jogador se lesiona, fica fora por N dias |
| `conflict` | Jogadores com química ruim brigam; moral cai para ambos |
| `inspiration` | Jogador aprende novo trait ou melhora familiaridade posicional |
| `fatigue` | Jogador treinou demais; stamina reduzida para próximo jogo |

### Evolução

- Atributos evoluem gradualmente com treino contínuo
- O tipo de sessão afeta quais atributos melhoram
- Jogadores jovens evoluem mais rápido
- Familiaridade posicional evolui ao treinar na posição nova

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
- **SVP** — segundo maior nota

### Visualização na Demo
Estilo **SofaScore**: campo esquemático com nota de cada jogador atualizada em tempo real, timeline de eventos lateral.

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
- Debuff do adversário (ex: adversário começa 2º tempo com moral reduzida)
- Inspiração de liderança (jogador com trait `leadership` inspira o grupo)

### Visual de Cartas
Estilo **3D hover** (referência: DaisyUI hover-3d): cartas com efeito de perspectiva ao interagir, brilho e raridade visual. Implementado em Godot, não web.

> ⚠️ **Decisão pendente:** Cartas de vestiário são **consumíveis** (gastas ao usar) ou **recorrentes** (disponíveis todo intervalo)? Esta decisão impacta diretamente o design da economia.

---

## 🎮 Táticas

- Sistema livre: posições customizáveis, pressão, largura, tempo de posse
- Mudanças táticas em tempo real durante a partida
- Substituições (até o limite regulamentar)
- Familiaridade tática evolui com treino

---

## 👔 Carreira do Técnico

### Estados

```
Empregado → Demitido → Desempregado → (Game Over se não encontrar clube)
                                     → (Convite para seleção se vencer mundial = Win)
```

### Causas de demissão
- Desempenho abaixo da expectativa do clube
- Desequilíbrio financeiro (falência)
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

### Win Condition (Demo)
- Vencer a liga nacional → classificar pro mundial → vencer o mundial
- Tela de vitória: convite para treinar a seleção do país do time escolhido
- Este é o **final positivo** da demo

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
| `training` | "Jovem talento tem breakthrough no treino" |

---

## 🎮 Demo Pré-Alpha (Itch.io)

### Escopo

| Item | Valor |
|---|---|
| Times | 32 (4 países × 8 times) |
| Competições | Liga eliminatória por país + Mundial entre campeões |
| Win condition | Vencer mundial → convite seleção |
| Game over | Falência e/ou demissão |
| Simulação visual | Eventos textuais + ratings SofaScore (sem gráfico) |
| Dados | Scrape manual único → JSON com nomes fictícios |
| Idiomas | PT-BR + EN |
| Twitch/Kick | Bilheteria por recompensas + youth avatares |
| Treinamento | Alocação + eventos aleatórios |

### O que NÃO entra na demo
- Simulação gráfica "futebol de botão" (apenas no release)
- Transferências completas (negociação detalhada)
- Mundo inteiro (~195 países)
- Seleções jogáveis
- Co-op / multiplayer
- UI gráfica complexa

---

## 🔮 Visão de Futuro

| Área | Plano |
|---|---|
| Plataformas | Steam, Switch, PS5, Mobile |
| Simulação gráfica | Estilo "futebol de botão" — 2D top-down com discos/sprites anime |
| Multiplayer | Co-op local, leaderboards online, possível modo online |
| Stream | Bot Node.js persistente, votações, eventos ao vivo |
| Idiomas | Meta: máxima cobertura (pesquisar idiomas mais usados em gaming) |
| Mundo | ~195 países com ligas, continentais, mundiais, seleções |
| Arte | Estilo anime com aparência de jogadores, estádios, cartas |

---

## 🎨 Estilo Visual

- **Arte:** Anime — representação fictícia e estilizada da realidade
- **Jogadores:** Inicialmente sem aparência detalhada; representados por cartas com stats
- **Cartas:** Efeito 3D hover (ref: DaisyUI hover-3d), brilho por raridade
- **Avatares Twitch:** Customizáveis estilo Cult of the Lamb
- **Partida (demo):** Sem gráfico de jogo — campo esquemático com notas (SofaScore)
- **Partida (release):** Estilo "futebol de botão" — campo 2D top-down com discos/sprites anime representando jogadores, movimentação simplificada
- **Ferramenta:** Aseprite para sprites e assets
