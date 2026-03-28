import { CardEffect } from "@/engine/enums";
import type { LockerRoomCard } from "@/engine/models";
import type { IRng } from "@/engine/simulation/rng";

/**
 * Generates contextual locker room cards at halftime.
 * Cards are recurrent (not consumable) -- drawn fresh each halftime.
 */

const CARD_POOL: LockerRoomCard[] = [
  {
    name: "Discurso Inspirador",
    description: "Moral do time +15",
    effect: CardEffect.MoraleBoost,
    magnitude: 15,
  },
  {
    name: "Pep Talk Individual",
    description: "Moral do jogador mais fraco +25",
    effect: CardEffect.MoraleBoost,
    magnitude: 25,
  },
  {
    name: "Garra e Raça",
    description: "Moral do time +10, Stamina +10",
    effect: CardEffect.MoraleBoost,
    magnitude: 10,
  },
  {
    name: "Hidratação Especial",
    description: "Stamina do time +20",
    effect: CardEffect.StaminaRecovery,
    magnitude: 20,
  },
  {
    name: "Recuperação Física",
    description: "Stamina dos 3 mais cansados +30",
    effect: CardEffect.StaminaRecovery,
    magnitude: 30,
  },
  {
    name: "Segundo Fôlego",
    description: "Stamina do time +15",
    effect: CardEffect.StaminaRecovery,
    magnitude: 15,
  },
  {
    name: "Ajuste Tático",
    description: "Buff de atributos +5 no 2° tempo",
    effect: CardEffect.TeamBuff,
    magnitude: 5,
  },
  {
    name: "Análise de Vídeo",
    description: "Buff de decisões +8 no 2° tempo",
    effect: CardEffect.TeamBuff,
    magnitude: 8,
  },
  {
    name: "Pressão Psicológica",
    description: "Moral do adversário -10",
    effect: CardEffect.OpponentDebuff,
    magnitude: 10,
  },
  {
    name: "Provocação Tática",
    description: "Moral do adversário -15",
    effect: CardEffect.OpponentDebuff,
    magnitude: 15,
  },
];

/**
 * Generates 3 contextual cards based on match state at halftime.
 * Cards are weighted by context: losing -> more recovery, winning -> more buffs.
 */
export function generate(
  rng: IRng,
  scoreDiff: number,
  avgStamina: number,
  avgMorale: number,
): LockerRoomCard[] {
  const weighted: Array<{ card: LockerRoomCard; weight: number }> = [];

  for (const card of CARD_POOL) {
    let weight = 10; // base weight

    // Losing: favor recovery and morale
    if (scoreDiff < 0) {
      if (
        card.effect === CardEffect.MoraleBoost ||
        card.effect === CardEffect.StaminaRecovery
      ) {
        weight += 15;
      }
    }
    // Winning: favor buffs and debuffs
    else if (scoreDiff > 0) {
      if (
        card.effect === CardEffect.TeamBuff ||
        card.effect === CardEffect.OpponentDebuff
      ) {
        weight += 15;
      }
    }

    // Low stamina: favor recovery
    if (avgStamina < 50) {
      if (card.effect === CardEffect.StaminaRecovery) {
        weight += 10;
      }
    }

    // Low morale: favor morale boost
    if (avgMorale < 40) {
      if (card.effect === CardEffect.MoraleBoost) {
        weight += 10;
      }
    }

    weighted.push({ card, weight });
  }

  // Weighted random selection of 3 unique cards
  const selected: LockerRoomCard[] = [];
  const available = [...weighted];

  for (let i = 0; i < 3 && available.length > 0; i++) {
    let totalWeight = 0;
    for (const w of available) {
      totalWeight += w.weight;
    }

    let roll = rng.nextInt(0, totalWeight - 1);
    let cumulative = 0;

    for (let j = 0; j < available.length; j++) {
      cumulative += available[j].weight;
      if (roll < cumulative) {
        selected.push(available[j].card);
        available.splice(j, 1);
        break;
      }
    }
  }

  return selected;
}
