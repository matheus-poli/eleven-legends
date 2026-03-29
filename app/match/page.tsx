"use client";

import { useState, useEffect, useCallback, useRef, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useGameStore } from "@/store/game-store";
import {
  GameLayout,
  PageWrapper,
  RatingBadge,
  OvrBadge,
  Badge,
  SoccerBallIcon,
  YellowCardIcon,
  RedCardIcon,
  SubstitutionIcon,
  GlovesIcon,
} from "@/components/ui";
import { MatchPitchView } from "@/components/pitch";
import { LiveMatchSession } from "@/engine/simulation/live-match-session";
import { type MatchConfig } from "@/engine/models/match-config";
import { type MatchState } from "@/engine/models/match-state";
import { type MatchEvent } from "@/engine/models/match-event";
import { type MatchDayContext } from "@/engine/game-state";
import { EventType } from "@/engine/enums/event-type";
import { TacticalStyle } from "@/engine/enums/tactical-style";
import { type Formation, F442, FORMATION_PRESETS } from "@/engine/models/formation";
import { averageOverall } from "@/engine/simulation/formation-optimizer";
import { type Substitution } from "@/engine/models/substitution";
import { type Player } from "@/engine/models/player";
import {
  ExclamationTriangleIcon,
  PlayIcon,
  PauseIcon,
} from "@heroicons/react/24/solid";
import {
  setMatchData,
  getMatchSession,
  getMatchConfig,
  getMatchContext,
  setMatchResult,
} from "@/store/match-store";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

type MatchPhaseUI = "prematch" | "playing" | "halftime-transition";

// ---------------------------------------------------------------------------
// Event icons / colors
// ---------------------------------------------------------------------------

function eventIcon(type: EventType): ReactNode {
  const base = "w-5 h-5";
  switch (type) {
    case EventType.Goal:
      return <SoccerBallIcon className={`${base} text-success`} />;
    case EventType.YellowCard:
      return <YellowCardIcon className="w-4 h-5" />;
    case EventType.RedCard:
      return <RedCardIcon className="w-4 h-5" />;
    case EventType.Foul:
      return <ExclamationTriangleIcon className={`${base} text-orange`} />;
    case EventType.Save:
      return <GlovesIcon className={`${base} text-info`} />;
    case EventType.Substitution:
      return <SubstitutionIcon className={`${base} text-orange`} />;
    default:
      return <span className="w-2 h-2 rounded-full bg-base-content/40 inline-block" />;
  }
}

function eventColor(type: EventType): string {
  switch (type) {
    case EventType.Goal:
      return "bg-success/10 border-success text-success";
    case EventType.YellowCard:
      return "bg-warning/10 border-warning text-warning";
    case EventType.RedCard:
      return "bg-error/10 border-error text-error";
    case EventType.Foul:
      return "bg-orange/10 border-orange text-orange";
    case EventType.Save:
      return "bg-info/10 border-info text-info";
    case EventType.Substitution:
      return "bg-orange/10 border-orange text-orange";
    default:
      return "bg-base-200 border-base-300 text-base-content/60";
  }
}

// ---------------------------------------------------------------------------
// Style button colors
// ---------------------------------------------------------------------------

const STYLE_META: Record<TacticalStyle, { label: string; badge: string }> = {
  [TacticalStyle.Attacking]: { label: "ATK", badge: "badge-error" },
  [TacticalStyle.Balanced]: { label: "BAL", badge: "badge-success" },
  [TacticalStyle.Defensive]: { label: "DEF", badge: "badge-info" },
};

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export default function MatchPage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.getState);

  // Core state
  const [phase, setPhase] = useState<MatchPhaseUI>("prematch");
  const [session, setSession] = useState<LiveMatchSession | null>(null);
  const [ctx, setCtx] = useState<MatchDayContext | null>(null);
  const [config, setConfig] = useState<MatchConfig | null>(null);
  const [events, setEvents] = useState<MatchEvent[]>([]);
  const [tick, setTick] = useState(0);

  // Playback controls
  const [tickInterval, setTickInterval] = useState(1000);
  const [paused, setPaused] = useState(false);

  // Tactical controls
  const [style, setStyle] = useState(TacticalStyle.Balanced);
  const [selectedFormation, setSelectedFormation] = useState<Formation>(F442);

  // Substitution panel
  const [showSubs, setShowSubs] = useState(false);
  const [subOut, setSubOut] = useState<number | null>(null);
  const [subsUsed, setSubsUsed] = useState(0);

  // Event feed scroll
  const feedRef = useRef<HTMLDivElement>(null);

  // On mount: check if resuming from halftime
  useEffect(() => {
    const existingSession = getMatchSession();
    if (existingSession && existingSession.isSecondHalf) {
      setSession(existingSession);
      setConfig(getMatchConfig());
      setCtx(getMatchContext());
      setEvents([...existingSession.state.events]);
      setPhase("playing");
    }
  }, []);

  // Auto-scroll event feed
  useEffect(() => {
    if (feedRef.current) {
      feedRef.current.scrollTop = feedRef.current.scrollHeight;
    }
  }, [events]);

  // Derived data
  const gs = (() => {
    try {
      return gameState();
    } catch {
      return null;
    }
  })();

  const playerClub = gs?.playerClub ?? null;
  const squad = playerClub?.team.players ?? [];

  // ---------------------------------------------------------------------------
  // Tick loop
  // ---------------------------------------------------------------------------

  useEffect(() => {
    if (phase !== "playing" || paused || !session) return;

    const id = setInterval(() => {
      const newEvents = session.processNextTick();
      if (newEvents.length > 0) {
        setEvents((prev) => [...prev, ...newEvents]);
      }
      setTick((t) => t + 1);

      if (session.isHalfTimeReached) {
        clearInterval(id);
        setPhase("halftime-transition");
      }
      if (session.isMatchFinished) {
        clearInterval(id);
        handleMatchFinished(session);
      }
    }, tickInterval);

    return () => clearInterval(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [phase, paused, session, tickInterval]);

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  const handleStartMatch = useCallback(() => {
    if (!gs || !playerClub) return;

    const matchCtx = gs.prepareMatchDay();
    if (!matchCtx.playerFixture) {
      router.push("/hub");
      return;
    }

    const isHome = matchCtx.playerFixture.homeClubId === gs.manager.clubId;
    const tactics = {
      formation: selectedFormation,
      style,
      startingPlayerIds: [
        ...playerClub.team.startingLineup,
      ] as readonly number[],
    };

    const matchConfig = gs.buildPlayerMatchConfig(
      matchCtx,
      isHome ? tactics : tactics,
    );

    const liveSession = new LiveMatchSession(matchConfig);

    setMatchData(liveSession, matchConfig, matchCtx);
    setSession(liveSession);
    setConfig(matchConfig);
    setCtx(matchCtx);
    setPhase("playing");
  }, [gs, playerClub, selectedFormation, style, router]);

  const handleGoToHalftime = useCallback(() => {
    router.push("/halftime");
  }, [router]);

  const handleMatchFinished = useCallback(
    (s: LiveMatchSession) => {
      const result = s.finalizeResult();
      setMatchResult(result);
      router.push("/post-match");
    },
    [router],
  );

  const handleSubstitute = useCallback(
    (playerInId: number) => {
      if (!session || subOut === null) return;

      const isHome =
        ctx?.playerFixture?.homeClubId === gs?.manager.clubId;
      const sub: Substitution = {
        playerOutId: subOut,
        playerInId,
      };

      const success = session.applySubstitution(sub, isHome ?? true);
      if (success) {
        setSubsUsed((n) => n + 1);
        setEvents((prev) => [
          ...prev,
          {
            tick: session.state.currentTick,
            type: EventType.Substitution,
            playerId: playerInId,
            secondaryPlayerId: subOut,
            description: `Substitution: player in`,
            ratingImpact: 0,
          },
        ]);
      }
      setSubOut(null);
      setShowSubs(false);
    },
    [session, subOut, ctx, gs],
  );

  // ---------------------------------------------------------------------------
  // Derived match data
  // ---------------------------------------------------------------------------

  const state: MatchState | null = session?.state ?? null;
  const homeTeam = config?.homeTeam ?? null;
  const awayTeam = config?.awayTeam ?? null;
  const isHome = ctx?.playerFixture?.homeClubId === gs?.manager.clubId;

  const possHome =
    state && state.totalTicksPlayed > 0
      ? Math.round((state.homePossessionTicks / state.totalTicksPlayed) * 100)
      : 50;
  const possAway = 100 - possHome;

  const matchMinute =
    state != null
      ? state.currentTick + (session?.isSecondHalf ? 45 : 0)
      : 0;

  // Active player IDs for the user's team
  const activePlayerIds = isHome
    ? state?.homeActivePlayerIds ?? []
    : state?.awayActivePlayerIds ?? [];

  const benchPlayers =
    squad.filter(
      (p) =>
        !activePlayerIds.includes(p.id),
    ) ?? [];

  // ---------------------------------------------------------------------------
  // Opponent data for prematch
  // ---------------------------------------------------------------------------

  const opponentClub = (() => {
    if (!gs || !ctx?.playerFixture) return null;
    const oppId = isHome
      ? ctx.playerFixture.awayClubId
      : ctx.playerFixture.homeClubId;
    return gs.clubs.find((c) => c.id === oppId) ?? null;
  })();

  // ---------------------------------------------------------------------------
  // Render: PreMatch
  // ---------------------------------------------------------------------------

  if (phase === "prematch") {
    return (
      <PageWrapper gradient="bg-gradient-to-b from-green/10 to-base-200">
        <div className="max-w-2xl mx-auto px-4 py-8">
          {/* Header */}
          <div className="text-center mb-8">
            <h1 className="text-3xl font-black mb-2">
              <span className="text-success">
                {isHome ? playerClub?.name : opponentClub?.name ?? "Away"}
              </span>
              <span className="text-base-content/40 mx-3">vs</span>
              <span className="text-secondary">
                {isHome ? opponentClub?.name ?? "Away" : playerClub?.name}
              </span>
            </h1>
            <p className="text-sm text-base-content/50">
              {isHome ? "Home" : "Away"} match
            </p>
          </div>

          {/* Formation Selector */}
          <div className="card bg-base-100 shadow-lg border border-base-300 mb-6">
            <div className="card-body">
              <h2 className="card-title text-lg">Formation</h2>
              <div className="flex flex-wrap gap-2">
                {FORMATION_PRESETS.map((f) => {
                  const ovr = averageOverall(squad, f);
                  const isSelected = f.name === selectedFormation.name;
                  return (
                    <button
                      key={f.name}
                      className={`btn btn-sm gap-2 ${
                        isSelected ? "btn-primary" : "btn-ghost"
                      }`}
                      onClick={() => setSelectedFormation(f)}
                    >
                      {f.name}
                      <OvrBadge ovr={ovr} size="xs" />
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Tactical Style */}
          <div className="card bg-base-100 shadow-lg border border-base-300 mb-6">
            <div className="card-body">
              <h2 className="card-title text-lg">Tactical Style</h2>
              <div className="flex gap-2">
                {(
                  [
                    TacticalStyle.Attacking,
                    TacticalStyle.Balanced,
                    TacticalStyle.Defensive,
                  ] as const
                ).map((s) => {
                  const meta = STYLE_META[s];
                  const isSelected = s === style;
                  return (
                    <button
                      key={s}
                      className={`btn flex-1 btn-raised ${
                        isSelected
                          ? s === TacticalStyle.Attacking
                            ? "btn-error"
                            : s === TacticalStyle.Balanced
                              ? "btn-success"
                              : "btn-info"
                          : "btn-ghost"
                      }`}
                      onClick={() => setStyle(s)}
                    >
                      {meta.label}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-3">
            <button
              className="btn btn-outline btn-secondary flex-1 btn-raised"
              onClick={() => router.push("/squad?returnUrl=/match")}
            >
              Edit Squad
            </button>
            <button
              className="btn btn-primary btn-lg flex-1 shadow-lg btn-raised font-bold"
              onClick={handleStartMatch}
            >
              Start Match!
            </button>
          </div>
        </div>
      </PageWrapper>
    );
  }

  // ---------------------------------------------------------------------------
  // Render: Halftime Transition
  // ---------------------------------------------------------------------------

  if (phase === "halftime-transition") {
    return (
      <PageWrapper gradient="bg-gradient-to-b from-neutral to-neutral/80">
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-center">
            <h1 className="text-5xl font-black text-white mb-4">HALFTIME</h1>
            <p className="text-2xl text-white/80 mb-2">
              {state?.scoreHome ?? 0} - {state?.scoreAway ?? 0}
            </p>
            <p className="text-sm text-white/40 mb-8">
              {homeTeam?.name} vs {awayTeam?.name}
            </p>
            <button
              className="btn btn-warning btn-lg shadow-lg btn-raised font-bold animate-pulse"
              onClick={handleGoToHalftime}
            >
              Locker Room
            </button>
          </div>
        </div>
      </PageWrapper>
    );
  }

  // ---------------------------------------------------------------------------
  // Render: Playing
  // ---------------------------------------------------------------------------

  return (
    <GameLayout
      header={
        // Score bar
        <div className="bg-base-300 border-b border-base-content/10 px-4 py-3">
          <div className="flex items-center justify-between max-w-5xl mx-auto">
            {/* Home */}
            <div className="flex items-center gap-2">
              <span
                className={`font-bold text-lg ${
                  isHome ? "text-primary" : "text-base-content"
                }`}
              >
                {homeTeam?.name ?? "Home"}
              </span>
            </div>

            {/* Score + minute */}
            <div className="flex items-center gap-3">
              <span className="text-3xl font-black tabular-nums">
                {state?.scoreHome ?? 0}
              </span>
              <div className="flex flex-col items-center">
                <span className="text-xs text-base-content/50">
                  {matchMinute}&apos;
                </span>
                <div className="w-24 h-1.5 bg-base-content/10 rounded-full overflow-hidden flex">
                  <div
                    className="h-full bg-primary transition-all duration-500"
                    style={{ width: `${possHome}%` }}
                  />
                  <div
                    className="h-full bg-secondary transition-all duration-500"
                    style={{ width: `${possAway}%` }}
                  />
                </div>
                <div className="flex justify-between w-24 text-[10px] text-base-content/40 tabular-nums">
                  <span>{possHome}%</span>
                  <span>{possAway}%</span>
                </div>
              </div>
              <span className="text-3xl font-black tabular-nums">
                {state?.scoreAway ?? 0}
              </span>
            </div>

            {/* Away */}
            <div className="flex items-center gap-2">
              <span
                className={`font-bold text-lg ${
                  !isHome ? "text-primary" : "text-base-content"
                }`}
              >
                {awayTeam?.name ?? "Away"}
              </span>
            </div>
          </div>
        </div>
      }
    >
      <div className="flex h-full">
        {/* Left: Pitch (60%) */}
        <div className="w-[60%] p-4 flex items-center justify-center overflow-hidden">
          {config && (
            <MatchPitchView
              config={config}
              homeFormation={selectedFormation}
              ratings={state?.playerRatings ?? {}}
              className="w-full max-h-full"
            />
          )}
        </div>

        {/* Right: Event Feed / Subs Panel (40%) */}
        <div className="w-[40%] flex flex-col border-l border-base-content/10 bg-base-200">
          {showSubs ? (
            // Substitution panel
            <div className="flex flex-col h-full">
              <div className="p-3 border-b border-base-content/10 flex items-center justify-between">
                <h3 className="font-bold">
                  Substitutions ({subsUsed}/3)
                </h3>
                <button
                  className="btn btn-ghost btn-sm"
                  onClick={() => {
                    setShowSubs(false);
                    setSubOut(null);
                  }}
                >
                  Back
                </button>
              </div>

              <div className="flex-1 overflow-auto p-3">
                {/* Active players (pick one to sub out) */}
                {subOut === null && (
                  <>
                    <p className="text-xs text-base-content/50 mb-2">
                      Select a player to take off:
                    </p>
                    {activePlayerIds.map((pid) => {
                      const player = squad.find((p) => p.id === pid);
                      if (!player) return null;
                      const rating = state?.playerRatings[pid] ?? 6.0;
                      const stamina = state?.playerStamina[pid] ?? 100;
                      return (
                        <button
                          key={pid}
                          className="w-full flex items-center gap-2 p-2 rounded-lg hover:bg-base-300 transition-colors mb-1"
                          onClick={() => setSubOut(pid)}
                          disabled={subsUsed >= 3}
                        >
                          <span className="font-medium text-sm flex-1 text-left truncate">
                            {player.name}
                          </span>
                          <span className="text-xs text-base-content/50">
                            {player.primaryPosition}
                          </span>
                          <RatingBadge rating={rating} size="xs" />
                          <span
                            className={`text-xs tabular-nums w-8 text-right ${
                              stamina < 40
                                ? "text-error"
                                : stamina < 60
                                  ? "text-warning"
                                  : "text-success"
                            }`}
                          >
                            {Math.round(stamina)}
                          </span>
                        </button>
                      );
                    })}
                  </>
                )}

                {/* Bench players (pick one to sub in) */}
                {subOut !== null && (
                  <>
                    <p className="text-xs text-base-content/50 mb-2">
                      Select replacement for{" "}
                      <strong>
                        {squad.find((p) => p.id === subOut)?.name}
                      </strong>
                      :
                    </p>
                    <button
                      className="btn btn-ghost btn-xs mb-2"
                      onClick={() => setSubOut(null)}
                    >
                      Cancel
                    </button>
                    {benchPlayers.map((player) => (
                      <button
                        key={player.id}
                        className="w-full flex items-center gap-2 p-2 rounded-lg hover:bg-base-300 transition-colors mb-1"
                        onClick={() => handleSubstitute(player.id)}
                      >
                        <span className="font-medium text-sm flex-1 text-left truncate">
                          {player.name}
                        </span>
                        <span className="text-xs text-base-content/50">
                          {player.primaryPosition}
                        </span>
                        <OvrBadge
                          ovr={player.attributes.stamina}
                          size="xs"
                        />
                      </button>
                    ))}
                  </>
                )}
              </div>
            </div>
          ) : (
            // Event feed
            <div className="flex flex-col h-full">
              <div
                ref={feedRef}
                className="flex-1 overflow-auto p-3 space-y-1.5"
              >
                {events.length === 0 && (
                  <p className="text-center text-base-content/30 text-sm mt-8">
                    Kick off...
                  </p>
                )}
                {events.map((evt, i) => (
                  <div
                    key={i}
                    className={`flex items-start gap-2 p-2 rounded-lg border text-sm animate-in fade-in ${eventColor(
                      evt.type,
                    )}`}
                  >
                    <span className="mt-0.5 shrink-0">
                      {eventIcon(evt.type)}
                    </span>
                    <div className="flex-1 min-w-0">
                      <p className="truncate">{evt.description}</p>
                      <p className="text-[10px] opacity-60">
                        {evt.tick + (session?.isSecondHalf ? 45 : 0)}&apos;
                      </p>
                    </div>
                  </div>
                ))}
              </div>

              {/* Control bar */}
              <div className="border-t border-base-content/10 p-3 space-y-2">
                {/* Speed controls */}
                <div className="flex gap-1">
                  <button
                    className={`btn btn-xs flex-1 ${paused ? "btn-warning" : "btn-ghost"}`}
                    onClick={() => setPaused(!paused)}
                  >
                    {paused ? (
                      <PlayIcon className="w-3.5 h-3.5" />
                    ) : (
                      <PauseIcon className="w-3.5 h-3.5" />
                    )}
                  </button>
                  {[
                    { label: "1x", ms: 1000 },
                    { label: "2x", ms: 500 },
                    { label: "4x", ms: 250 },
                    { label: "8x", ms: 125 },
                  ].map((speed) => (
                    <button
                      key={speed.label}
                      className={`btn btn-xs flex-1 ${
                        tickInterval === speed.ms && !paused
                          ? "btn-primary"
                          : "btn-ghost"
                      }`}
                      onClick={() => {
                        setTickInterval(speed.ms);
                        setPaused(false);
                      }}
                    >
                      {speed.label}
                    </button>
                  ))}
                </div>

                {/* Style + subs */}
                <div className="flex gap-1">
                  {(
                    [
                      TacticalStyle.Attacking,
                      TacticalStyle.Balanced,
                      TacticalStyle.Defensive,
                    ] as const
                  ).map((s) => {
                    const meta = STYLE_META[s];
                    return (
                      <button
                        key={s}
                        className={`btn btn-xs flex-1 ${
                          style === s ? meta.badge : "btn-ghost"
                        }`}
                        onClick={() => setStyle(s)}
                      >
                        {meta.label}
                      </button>
                    );
                  })}
                  <button
                    className={`btn btn-xs flex-1 ${
                      showSubs ? "btn-accent" : "btn-ghost"
                    }`}
                    onClick={() => setShowSubs(true)}
                    disabled={subsUsed >= 3}
                  >
                    Subs ({subsUsed}/3)
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </GameLayout>
  );
}
