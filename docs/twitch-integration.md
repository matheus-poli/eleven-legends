# Integração Twitch/Kick

---

## 🎯 Visão Geral

A integração com Twitch e Kick conecta o jogo ao público de stream de duas formas:

1. **Youth Avatars** — espectadores do chat se tornam jogadores de base com aparência customizável
2. **Bilheteria por Recompensas** — viewers resgatam recompensas que geram receita para o clube

---

## 🧍 Youth Avatars (Jogadores de Base do Chat)

### Conceito

Cada espectador do chat pode ser vinculado a um **jogador de categoria de base** no jogo. O youth avatar:

- Tem uma **aparência customizável** estilo **Cult of the Lamb** (cabeça, corpo, acessórios, cores)
- Aparece na lista de "torcedores" e na categoria de base do clube
- Pode evoluir e eventualmente ser promovido ao time principal (decisão do técnico)
- O viewer pode customizar a aparência via extensão de Twitch/Kick ou comandos de chat

### Mecânica

```
Viewer entra no chat
 └─ Extensão/comando cria youth avatar vinculado ao username
      └─ Avatar aparece na torcida virtual e na base do clube
           └─ Viewer pode customizar aparência (extensão)
           └─ Se o técnico promover o youth → viewer vira "dono" do jogador
```

### Customização de Aparência

Inspirado em **Cult of the Lamb** Twitch Extension:

| Categoria | Opções |
|---|---|
| Cabeça | Formato, cabelo, cor do cabelo |
| Rosto | Olhos, boca, expressão |
| Corpo | Uniforme, número, cor |
| Acessórios | Chapéu, óculos, faixa |
| Especial | Efeitos de raridade (brilho, aura) |

### Atributos do Youth Avatar

Youth players gerados via chat têm atributos baseados em:
- **Potencial aleatório** (RNG seeded pelo username)
- **Engajamento do viewer** (mais mensagens → bônus de moral)
- **Tempo no chat** (viewers antigos têm youth mais desenvolvido)

> ⚠️ O youth avatar é uma feature de engajamento — não deve quebrar o balanceamento. Jogadores gerados por chat não devem ser consistentemente melhores ou piores que os gerados proceduralmente.

---

## 🎟️ Bilheteria por Recompensas

### Conceito

Viewers podem resgatar **recompensas de canal** (Channel Points no Twitch, equivalente no Kick) que se traduzem em receita de bilheteria para o clube no jogo.

### Mecânica

```
Viewer resgata recompensa "Comprar Ingresso"
 └─ Bot registra o resgate
      └─ Jogo soma ao ticket_revenue da próxima partida
           └─ club_balance += ticket_revenue no fim do jogo
```

### Tipos de Recompensa

| Recompensa | Custo (Channel Points) | Efeito no Jogo |
|---|---|---|
| `buy_ticket` | Baixo | +receita de bilheteria básica |
| `buy_vip_ticket` | Médio | +receita premium |
| `buy_season_pass` | Alto | +receita recorrente por N jogos |
| `sponsor_youth` | Médio | +bônus de moral para youth avatar do viewer |

### Fórmula de Bilheteria

```
ticket_revenue = (base_attendance * ticket_price) + sum(twitch_ticket_values)
```

Na demo, `ticket_price` é fixo. Ajuste de preço é feature futura.

---

## 🏗️ Arquitetura Técnica

### Na Demo (Básico)

```
[Twitch/Kick Chat] ←→ [Bot (GDScript WebSocket)] ←→ [Jogo Godot]
```

- Bot direto em GDScript via `WebSocketClient`
- Conecta ao Twitch IRC (ou Kick chat)
- Processa comandos e recompensas
- Atualiza estado do jogo em tempo real

### No Futuro (Persistente)

```
[Twitch/Kick] ←→ [Bot Node.js 24/7] ←→ [API REST] ←→ [Jogo Godot]
```

- Bot Node.js roda independentemente do jogo
- Acumula dados de viewers (engajamento, recompensas)
- Sincroniza via API quando o jogo inicia

---

## 🔌 Protocolo de Comunicação

### Twitch IRC (Demo)

```
# Conectar ao Twitch IRC
ws://irc-ws.chat.twitch.tv:443

# Comandos processados pelo bot
!join          → Cria youth avatar para o viewer
!customize     → Abre link para extensão de customização
!stats         → Mostra stats do youth avatar do viewer
!ticket        → Resgate de recompensa (Channel Points)
```

### Eventos do Bot → Jogo

```gdscript
signal viewer_joined(username: String)
signal ticket_purchased(username: String, tier: String)
signal avatar_customized(username: String, config: Dictionary)
signal youth_promoted(username: String, player_id: String)
```

---

## 🎯 Escopo na Demo

| Feature | Status |
|---|---|
| Youth avatars vinculados ao chat | ✅ Básico |
| Customização de aparência | ✅ Limitada (poucas opções) |
| Bilheteria por recompensas | ✅ Básico (`buy_ticket` apenas) |
| Bot GDScript WebSocket | ✅ Direto no jogo |
| Extensão de Twitch completa | ❌ Futuro |
| Bot Node.js persistente | ❌ Futuro |
| Votações de chat | ❌ Futuro |
| Eventos ao vivo (chat influencia partida) | ❌ Futuro |
| Season pass / VIP | ❌ Futuro |

---

## 🔮 Futuro

### Votações de Chat
- Chat vota em decisões táticas (substituição, formação)
- Chat pode "torcer" durante a partida (afeta moral)
- Polls aparecem na tela do stream

### Eventos ao Vivo
- Chat acumula "energia" → desbloqueia evento especial (ex: torcida empurra o time no final)
- Raids de outros canais → chegada de público extra (boost de bilheteria)
- Subscribers têm youth avatars com visual especial

### Extensão de Twitch
- Painel de customização completo do youth avatar
- Visualização dos stats do jogador vinculado
- Histórico de partidas e gols
- Ranking de viewers (quem tem o melhor youth player)

### Kick Integration
- Equivalente ao Twitch, adaptado para a API do Kick
- Comandos de chat compatíveis
- Sistema de recompensas próprio do Kick