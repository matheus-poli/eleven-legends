import { type LiveMatchSession } from "@/engine/simulation/live-match-session";
import { type MatchConfig } from "@/engine/models/match-config";
import { type MatchResult } from "@/engine/models/match-result";
import { type MatchDayContext } from "@/engine/game-state";

// Module-level state for passing data between match pages
// (match -> halftime -> match -> post-match).
// This survives client-side navigation but not full page reloads.

let _session: LiveMatchSession | null = null;
let _config: MatchConfig | null = null;
let _context: MatchDayContext | null = null;
let _result: MatchResult | null = null;

export function getMatchSession(): LiveMatchSession | null {
  return _session;
}

export function getMatchConfig(): MatchConfig | null {
  return _config;
}

export function getMatchContext(): MatchDayContext | null {
  return _context;
}

export function getMatchResult(): MatchResult | null {
  return _result;
}

export function setMatchData(
  session: LiveMatchSession,
  config: MatchConfig,
  ctx: MatchDayContext,
): void {
  _session = session;
  _config = config;
  _context = ctx;
}

export function setMatchResult(result: MatchResult): void {
  _result = result;
}

export function clearMatch(): void {
  _session = null;
  _config = null;
  _context = null;
  _result = null;
}
