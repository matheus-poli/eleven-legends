import { DayType } from "@/engine/enums";
import type { MatchFixture } from "@/engine/models";

/**
 * Represents a single day in the season calendar.
 */
export interface SeasonDay {
  day: number;
  type: DayType;
  fixtures: readonly MatchFixture[];
}

/**
 * Builds the full season calendar: national knockouts -> mundial.
 * Structure: Training -> Quarters -> Training -> Semis -> Training -> Final ->
 *            Rest -> Transfer Window (5 days) -> Rest ->
 *            Training -> Mundial Semis -> Training -> Mundial Final
 */
export function buildTemplate(): SeasonDay[] {
  const days: SeasonDay[] = [];
  let day = 1;

  // Phase 1: National knockouts
  pushTrainingBlock(days, day, 3);
  day += 3;
  days.push({ day: day++, type: DayType.MatchDay, fixtures: [] }); // Quarterfinals

  pushTrainingBlock(days, day, 2);
  day += 2;
  days.push({ day: day++, type: DayType.MatchDay, fixtures: [] }); // Semifinals

  pushTrainingBlock(days, day, 2);
  day += 2;
  days.push({ day: day++, type: DayType.MatchDay, fixtures: [] }); // Finals

  // Transfer window between nationals and mundial
  days.push({ day: day++, type: DayType.Rest, fixtures: [] }); // Rest before window
  for (let i = 0; i < 5; i++) {
    days.push({ day: day++, type: DayType.TransferWindow, fixtures: [] });
  }
  days.push({ day: day++, type: DayType.Rest, fixtures: [] }); // Rest after window

  // Phase 2: Mundial
  pushTrainingBlock(days, day, 3);
  day += 3;
  days.push({ day: day++, type: DayType.MundialMatchDay, fixtures: [] }); // Mundial Semis

  pushTrainingBlock(days, day, 2);
  day += 2;
  days.push({ day: day++, type: DayType.MundialMatchDay, fixtures: [] }); // Mundial Final

  return days;
}

function pushTrainingBlock(
  days: SeasonDay[],
  startDay: number,
  count: number,
): void {
  for (let i = 0; i < count; i++) {
    days.push({ day: startDay + i, type: DayType.Training, fixtures: [] });
  }
}
