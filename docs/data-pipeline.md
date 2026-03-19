# Data Pipeline — Scraper e Gerador de Nomes Fictícios

---

## 🎯 Objetivo

O mundo de Eleven Legends é **fictício mas baseado na realidade**. Para isso, precisamos de um pipeline que:

1. **Coleta** dados de futebol real de diversas fontes
2. **Normaliza** em um formato unificado
3. **Gera nomes fictícios** algoritmicamente
4. **Exporta** JSON estruturado para o jogo consumir

---

## 📊 Fontes de Dados

| Fonte | Dados disponíveis | Notas |
|---|---|---|
| [TransferMarkt](https://www.transfermarkt.com) | Jogadores, valores, transferências, elencos | Requer scraping (sem API pública oficial) |
| [FBref](https://fbref.com) | Estatísticas detalhadas por jogo/temporada | Dados de StatsBomb, boa cobertura |
| [SofaScore](https://www.sofascore.com) | Ratings, eventos, lineups | API semi-pública |
| [Football-Data.org](https://www.football-data.org) | Ligas, tabelas, resultados | API gratuita com limites |
| [API-Football](https://www.api-football.com) | Dados abrangentes | API paga, boa cobertura |

### Prioridade para a Demo
- **TransferMarkt** para elencos e valores
- **FBref** para stats de jogadores
- Execução **manual única**: rodar o scraper, salvar JSON, não precisa de pipeline automatizado

---

## 🔄 Pipeline

```
[Fontes de dados reais]
        │
        ▼
  scrape_data.py          ← Coleta dados brutos
        │
        ▼
  normalize_data.py       ← Unifica formato, preenche gaps
        │
        ▼
  generate_names.py       ← Gera nomes fictícios (anagramas/sílabas)
        │
        ▼
  export_json.py          ← Exporta JSON final para o jogo
        │
        ▼
  data/teams.json         ← Consumido pelo Godot
  data/players.json
  data/leagues.json
```

### Localização no repo
Todos os scripts ficam em `tools/data-pipeline/`. Não são GDScript — usar **Python** (melhor ecossistema para scraping/data).

---

## 🎭 Algoritmo de Nomes Fictícios

### Requisitos
- Nomes devem ser **reconhecíveis** como referência ao real, mas **legalmente distintos**
- Deve funcionar para nomes de **jogadores**, **times**, **ligas** e **países**
- Resultados devem ser **determinísticos** (mesma entrada → mesma saída)

### Estratégia: Anagramas + Sílabas Embaralhadas

```python
def generate_fictional_name(real_name: str, seed: int) -> str:
    """
    Gera nome fictício a partir do real.
    
    Estratégia:
    1. Divide o nome em sílabas
    2. Embaralha sílabas com seed determinística
    3. Recombina mantendo estrutura fonética plausível
    4. Preserva iniciais quando possível (reconhecibilidade)
    """
```

### Exemplos conceituais

| Real | Fictício (exemplo) | Técnica |
|---|---|---|
| Lionel Messi | Leonil Isems | Anagrama parcial |
| Real Madrid | Rale Dimard | Sílabas embaralhadas |
| Premier League | Primeer Leageu | Troca de vogais |
| Brasil | Barsil | Anagrama simples |
| Neymar Jr. | Reynam Jr. | Sílabas invertidas |

> **Nota:** os exemplos acima são ilustrativos. O algoritmo real deve ser testado para garantir que os nomes sejam pronunciáveis e não idênticos aos reais.

### Regras
1. **Nomes de países** devem ser diferentes o suficiente para não confundir, mas reconhecíveis
2. **Nomes de times** podem ser mais próximos (paródia é permitida)
3. **Nomes de jogadores** devem manter sonoridade similar
4. **Seed fixa** por entidade: mesmo jogador/time sempre gera o mesmo nome fictício
5. Um **mapeamento real→fictício** deve ser exportado para referência interna

---

## 📦 Schema JSON de Saída

### `leagues.json`

```json
[
  {
    "id": "league_001",
    "real_ref": "Premier League",
    "name": "Primeer Leageu",
    "country_id": "country_eng",
    "tier": 1,
    "teams": ["team_001", "team_002", "..."]
  }
]
```

### `teams.json`

```json
[
  {
    "id": "team_001",
    "real_ref": "Manchester United",
    "name": "Manchestar Untied",
    "league_id": "league_001",
    "country_id": "country_eng",
    "reputation": 85,
    "balance": 150000000,
    "stadium_capacity": 75000,
    "players": ["player_001", "player_002", "..."]
  }
]
```

### `players.json`

```json
[
  {
    "id": "player_001",
    "real_ref": "Marcus Rashford",
    "name": "Marcsu Rashfrod",
    "team_id": "team_001",
    "country_id": "country_eng",
    "age": 26,
    "position_primary": "LW",
    "position_secondary": ["ST", "RW"],
    "attributes": {
      "finishing": 72,
      "passing": 65,
      "dribbling": 78,
      "first_touch": 70,
      "technique": 71,
      "decisions": 64,
      "composure": 66,
      "positioning": 70,
      "anticipation": 63,
      "off_ball": 72,
      "speed": 90,
      "acceleration": 88,
      "stamina": 75,
      "strength": 68,
      "agility": 82,
      "consistency": 60,
      "leadership": 45,
      "flair": 75,
      "big_matches": 65
    },
    "traits": ["Close Control", "Finesse Shot"],
    "potential": 82,
    "value": 45000000,
    "salary_weekly": 200000
  }
]
```

### Notas sobre o schema
- `real_ref` é **apenas para referência interna** (debug/mapeamento), nunca exposto ao jogador
- `potential` é oculto do jogador (só visível parcialmente via olheiros)
- Atributos de goleiro (`reflexes`, `handling`, `gk_positioning`, `aerial`) são adicionais para GKs

---

## 🎯 Escopo na Demo

| Item | Status |
|---|---|
| Scraper automatizado | ❌ Não necessário |
| Execução manual única | ✅ Rodar scripts, salvar JSON |
| 4 países × 8 times | ✅ 32 times com ~20 jogadores cada |
| Nomes fictícios | ✅ Gerados algoritmicamente |
| Pipeline automático | ❌ Futuro (atualização periódica) |

### Quantidades na Demo
- **4 países** (sugestão: equivalentes fictícios de Brasil, Inglaterra, Espanha, Alemanha)
- **8 times por país** = 32 times
- **~20 jogadores por time** = ~640 jogadores
- **1 JSON estático** por categoria (leagues, teams, players)

---

## 🔮 Futuro

- Pipeline automatizado (cron job semanal/mensal)
- Cobertura de ~195 países
- Atualização de stats ao longo das temporadas reais
- Geração de jogadores de categoria de base via procedural (não scrape)
- API interna para consultar dados durante o jogo