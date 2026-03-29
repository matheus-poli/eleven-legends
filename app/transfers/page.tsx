"use client";

import { useState, useReducer, useMemo, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useGameStore } from "@/store/game-store";
import { PageWrapper, GameLayout, OvrBadge, Money, formatMoney, Flag, StatChip } from "@/components/ui";
import { getAvailablePlayers, getSellablePlayers, executeBuy, addFreeAgent, salaryReserve } from "@/engine/transfers/transfer-market";
import { MAX_SQUAD_SIZE } from "@/engine/transfers/transfer-market";
import { calculate as calculateValuation } from "@/engine/transfers/player-valuation";
import { getRegions, scout } from "@/engine/transfers/scouting-system";
import { generateProspects } from "@/engine/transfers/youth-academy";
import { TransferType } from "@/engine/enums/transfer-type";
import { Position } from "@/engine/enums/position";
import { type Player } from "@/engine/models/player";
import { type Club } from "@/engine/models/club";
import { type ScoutRegion } from "@/engine/models/scout-region";
import { outfieldOverall, goalkeeperOverall } from "@/engine/models/player-attributes";
import { SeededRng } from "@/engine/simulation/rng";
import {
  MagnifyingGlassIcon,
  NoSymbolIcon,
  AcademicCapIcon,
  CheckCircleIcon,
} from "@heroicons/react/24/solid";

/* ------------------------------------------------------------------ */
/*  Constants                                                          */
/* ------------------------------------------------------------------ */

const POSITION_FILTERS = [
  "All", "GK", "CB", "LB", "RB", "CDM", "CM", "CAM", "LW", "RW", "ST",
] as const;

const YOUTH_COST = 10_000;

type Tab = "buy" | "sell" | "youth" | "scout";

/* ================================================================== */
/*  Transfers Page                                                     */
/* ================================================================== */

export default function TransfersPage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.gameState);

  const [tab, setTab] = useState<Tab>("buy");
  const [filterPos, setFilterPos] = useState("All");
  const [filterCountry, setFilterCountry] = useState("All");
  const [youthGenerated, setYouthGenerated] = useState(false);
  const [youthProspects, setYouthProspects] = useState<Array<{ prospect: Player; fee: number }>>([]);
  const [scoutResults, setScoutResults] = useState<Array<{ player: Player; signFee: number }>>([]);
  const [scoutedRegion, setScoutedRegion] = useState<string | null>(null);
  const [_, forceUpdate] = useReducer((x: number) => x + 1, 0);

  const clubs = gameState?.clubs ?? [];
  const club = gameState?.playerClub ?? null;

  // Collect all unique countries for the country filter
  const allCountries = useMemo(() => {
    const set = new Set<string>();
    for (const c of clubs) {
      set.add(c.country);
    }
    return ["All", ...Array.from(set).sort()];
  }, [clubs]);

  /* ---- Buy data -------------------------------------------------- */

  const buyList = useMemo(() => {
    if (!club) return [];
    const raw = getAvailablePlayers(clubs, playerClub.id);
    return raw.filter((item) => {
      if (filterPos !== "All" && item.player.primaryPosition !== filterPos) return false;
      if (filterCountry !== "All" && item.club.country !== filterCountry) return false;
      return true;
    });
  }, [clubs, club, filterPos, filterCountry]);

  /* ---- Sell data ------------------------------------------------- */

  const sellList = useMemo(() => {
    if (!club) return [];
    const raw = club ? getSellablePlayers(club) : [];
    return raw.filter((item) => {
      if (filterPos !== "All" && item.player.primaryPosition !== filterPos) return false;
      return true;
    });
  }, [club, filterPos]);

  useEffect(() => {
    if (!gameState) { router.replace("/"); }
  }, [gameState, router]);

  if (!gameState || !club) return null;

  const gs = gameState;
  const playerClub = club!;
  const budget = playerClub.balance;
  const squadSize = playerClub.team.players.length;
  const reserve = salaryReserve(playerClub);

  /* ---- Handlers -------------------------------------------------- */

  function handleBuy(player: Player, sellerClub: Club, price: number) {
    const success = executeBuy(playerClub, sellerClub, player, price);
    if (success) {
      gs.recordTransfer({
        type: TransferType.Buy,
        playerId: player.id,
        playerName: player.name,
        fromClubId: sellerClub.id,
        toClubId: playerClub.id,
        fee: price,
        day: gs.currentDayIndex,
      });
    }
    forceUpdate();
  }

  function handleSell(player: Player, price: number) {
    // Find a buyer club (the one with the highest balance that isn't at max squad)
    const buyers = (gs.clubs as Club[]).filter(
      (c) => c.id !== playerClub.id && c.team.players.length < MAX_SQUAD_SIZE,
    );
    if (buyers.length === 0) return;
    const buyer = buyers.reduce((best, c) => (c.balance > best.balance ? c : best), buyers[0]);

    const success = executeBuy(buyer, playerClub, player, price);
    if (success) {
      gs.recordTransfer({
        type: TransferType.Sell,
        playerId: player.id,
        playerName: player.name,
        fromClubId: playerClub.id,
        toClubId: buyer.id,
        fee: price,
        day: gs.currentDayIndex,
      });
    }
    forceUpdate();
  }

  function handleGenerateYouth() {
    if (youthGenerated) return;
    if (playerClub.balance < YOUTH_COST) return;

    playerClub.balance -= YOUTH_COST;
    const rng = new SeededRng(gs.baseSeed + gs.currentDayIndex * 3333);
    const nextId = gs.getNextPlayerId(3);
    const prospects = generateProspects(rng, playerClub.country, nextId);
    setYouthProspects(prospects);
    setYouthGenerated(true);
    forceUpdate();
  }

  function handleRecruitYouth(prospect: Player, fee: number) {
    const success = addFreeAgent(playerClub, prospect, fee);
    if (success) {
      gs.recordTransfer({
        type: TransferType.YouthRecruit,
        playerId: prospect.id,
        playerName: prospect.name,
        fromClubId: null,
        toClubId: playerClub.id,
        fee,
        day: gs.currentDayIndex,
      });
      setYouthProspects((prev) => prev.filter((p) => p.prospect.id !== prospect.id));
    }
    forceUpdate();
  }

  function handleScout(region: ScoutRegion) {
    if (playerClub.balance < region.cost) return;

    playerClub.balance -= region.cost;
    const rng = new SeededRng(gs.baseSeed + gs.currentDayIndex * 5555 + region.cost);
    const nextId = gs.getNextPlayerId(5);
    const results = scout(rng, region, nextId);
    setScoutResults(results);
    setScoutedRegion(region.name);
    forceUpdate();
  }

  function handleSignScout(player: Player, signFee: number) {
    const success = addFreeAgent(playerClub, player, signFee);
    if (success) {
      gs.recordTransfer({
        type: TransferType.ScoutRecruit,
        playerId: player.id,
        playerName: player.name,
        fromClubId: null,
        toClubId: playerClub.id,
        fee: signFee,
        day: gs.currentDayIndex,
      });
      setScoutResults((prev) => prev.filter((r) => r.player.id !== player.id));
    }
    forceUpdate();
  }

  /* ---- Tab styling ----------------------------------------------- */

  const tabStyles: Record<Tab, { active: string; inactive: string }> = {
    buy: { active: "tab-active text-green-600", inactive: "" },
    sell: { active: "tab-active text-red-600", inactive: "" },
    youth: { active: "tab-active text-blue-600", inactive: "" },
    scout: { active: "tab-active text-purple-600", inactive: "" },
  };

  /* ================================================================ */
  /*  Render                                                           */
  /* ================================================================ */

  return (
    <PageWrapper>
      <GameLayout
        /* ---- Header ---- */
        header={
          <div className="bg-base-200 border-b border-base-300">
            {/* Title + budget row */}
            <div className="flex items-center justify-between px-4 py-3">
              <h1 className="text-xl font-black">Transfer Window</h1>
              <div className="flex gap-2">
                <div className="badge badge-success badge-lg font-bold gap-1">
                  <Money amount={budget} /> Budget
                </div>
                <div className="badge badge-info badge-lg font-bold">
                  {squadSize}/{MAX_SQUAD_SIZE} Squad
                </div>
              </div>
            </div>

            {/* Tab bar */}
            <div role="tablist" className="tabs tabs-bordered px-4">
              {(["buy", "sell", "youth", "scout"] as Tab[]).map((t) => (
                <button
                  key={t}
                  role="tab"
                  className={`tab font-bold capitalize ${
                    tab === t ? tabStyles[t].active : tabStyles[t].inactive
                  }`}
                  onClick={() => setTab(t)}
                >
                  {t === "buy" && <><span className="w-2 h-2 rounded-full bg-green inline-block" /> Buy</>}
                  {t === "sell" && <><span className="w-2 h-2 rounded-full bg-red inline-block" /> Sell</>}
                  {t === "youth" && <><span className="w-2 h-2 rounded-full bg-blue inline-block" /> Youth</>}
                  {t === "scout" && <><span className="w-2 h-2 rounded-full bg-purple inline-block" /> Scout</>}
                </button>
              ))}
            </div>

            {/* Filter row (buy/sell tabs only) */}
            {(tab === "buy" || tab === "sell") && (
              <div className="flex flex-wrap gap-1.5 px-4 py-2 border-t border-base-300">
                {POSITION_FILTERS.map((pos) => (
                  <button
                    key={pos}
                    className={`btn btn-xs ${
                      filterPos === pos
                        ? "btn-primary"
                        : "btn-ghost bg-base-100"
                    }`}
                    onClick={() => setFilterPos(pos)}
                  >
                    {pos}
                  </button>
                ))}
                {tab === "buy" && (
                  <>
                    <div className="divider divider-horizontal mx-1" />
                    {allCountries.map((country) => (
                      <button
                        key={country}
                        className={`btn btn-xs ${
                          filterCountry === country
                            ? "btn-secondary"
                            : "btn-ghost bg-base-100"
                        }`}
                        onClick={() => setFilterCountry(country)}
                      >
                        {country !== "All" ? (
                          <span className="flex items-center gap-1">
                            <Flag country={country} size="sm" /> {country}
                          </span>
                        ) : (
                          "All"
                        )}
                      </button>
                    ))}
                  </>
                )}
              </div>
            )}
          </div>
        }
        /* ---- Footer ---- */
        footer={
          <div className="flex gap-2 p-3 border-t border-base-300 bg-base-100">
            <button
              className="btn btn-outline flex-1"
              onClick={() => router.push("/hub")}
            >
              Back to Hub
            </button>
            <button
              className="btn btn-info flex-1"
              onClick={() => router.push("/squad")}
            >
              Squad
            </button>
          </div>
        }
      >
        {/* ---- Tab content ---- */}
        <div className="p-4 max-w-4xl mx-auto w-full">
          {tab === "buy" && (
            <BuyTab
              items={buyList}
              club={club}
              reserve={reserve}
              onBuy={handleBuy}
            />
          )}

          {tab === "sell" && (
            <SellTab items={sellList} onSell={handleSell} />
          )}

          {tab === "youth" && (
            <YouthTab
              generated={youthGenerated}
              prospects={youthProspects}
              club={club}
              onGenerate={handleGenerateYouth}
              onRecruit={handleRecruitYouth}
            />
          )}

          {tab === "scout" && (
            <ScoutTab
              club={club}
              scoutResults={scoutResults}
              scoutedRegion={scoutedRegion}
              onScout={handleScout}
              onSign={handleSignScout}
            />
          )}
        </div>
      </GameLayout>
    </PageWrapper>
  );
}

/* ================================================================== */
/*  Buy Tab                                                            */
/* ================================================================== */

function BuyTab({
  items,
  club,
  reserve,
  onBuy,
}: {
  items: Array<{ player: Player; club: Club; price: number }>;
  club: Club;
  reserve: number;
  onBuy: (player: Player, seller: Club, price: number) => void;
}) {
  if (items.length === 0) {
    return (
      <div className="text-center text-base-content/40 py-16">
        <MagnifyingGlassIcon className="w-10 h-10 mx-auto mb-2 text-base-content/30" />
        <p>No players match your filters.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      {items.map(({ player, club: sellerClub, price }) => {
        const canAfford = club.balance - price >= reserve;
        const squadFull = club.team.players.length >= MAX_SQUAD_SIZE;
        const disabled = !canAfford || squadFull;

        return (
          <PlayerCard
            key={player.id}
            player={player}
            subtitle={`${player.primaryPosition} | Age ${player.age} | ${sellerClub.name}`}
            price={price}
            actionLabel="Buy"
            actionColor="btn-success"
            disabled={disabled}
            disabledReason={
              squadFull ? "Squad full" : !canAfford ? "Can't afford" : undefined
            }
            onAction={() => onBuy(player, sellerClub, price)}
          />
        );
      })}
    </div>
  );
}

/* ================================================================== */
/*  Sell Tab                                                           */
/* ================================================================== */

function SellTab({
  items,
  onSell,
}: {
  items: Array<{ player: Player; price: number }>;
  onSell: (player: Player, price: number) => void;
}) {
  if (items.length === 0) {
    return (
      <div className="text-center text-base-content/40 py-16">
        <NoSymbolIcon className="w-10 h-10 mx-auto mb-2 text-base-content/30" />
        <p>No players available to sell (minimum squad size reached).</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      {items.map(({ player, price }) => (
        <PlayerCard
          key={player.id}
          player={player}
          subtitle={`${player.primaryPosition} | Age ${player.age}`}
          price={price}
          actionLabel="Sell"
          actionColor="btn-error"
          disabled={false}
          onAction={() => onSell(player, price)}
        />
      ))}
    </div>
  );
}

/* ================================================================== */
/*  Youth Tab                                                          */
/* ================================================================== */

function YouthTab({
  generated,
  prospects,
  club,
  onGenerate,
  onRecruit,
}: {
  generated: boolean;
  prospects: Array<{ prospect: Player; fee: number }>;
  club: Club;
  onGenerate: () => void;
  onRecruit: (prospect: Player, fee: number) => void;
}) {
  return (
    <div>
      {/* Generate button */}
      {!generated && (
        <div className="text-center py-12">
          <AcademicCapIcon className="w-12 h-12 mx-auto mb-4 text-blue" />
          <h2 className="text-xl font-bold mb-2">Youth Academy</h2>
          <p className="text-base-content/60 mb-6">
            Generate 3 youth prospects for your academy.
            <br />
            <span className="font-semibold">Cost: {formatMoney(YOUTH_COST)}</span>
            <span className="text-base-content/40"> (1 per transfer window)</span>
          </p>
          <button
            className="btn btn-info btn-lg font-bold shadow-md btn-raised"
            onClick={onGenerate}
            disabled={club.balance < YOUTH_COST}
          >
            Generate Prospects
          </button>
          {club.balance < YOUTH_COST && (
            <p className="text-red-500 text-sm mt-2">Not enough funds</p>
          )}
        </div>
      )}

      {/* Prospects list */}
      {generated && prospects.length > 0 && (
        <div className="flex flex-col gap-3">
          <p className="text-sm text-base-content/50 font-medium mb-1">
            Youth Prospects — pick who to recruit:
          </p>
          {prospects.map(({ prospect, fee }) => {
            const squadFull = club.team.players.length >= MAX_SQUAD_SIZE;
            const canAfford = fee === 0 || club.balance >= fee;
            return (
              <PlayerCard
                key={prospect.id}
                player={prospect}
                subtitle={`${prospect.primaryPosition} | Age ${prospect.age} | Youth`}
                price={fee}
                actionLabel="Recruit"
                actionColor="btn-info"
                disabled={squadFull || !canAfford}
                disabledReason={
                  squadFull ? "Squad full" : !canAfford ? "Can't afford" : undefined
                }
                onAction={() => onRecruit(prospect, fee)}
              />
            );
          })}
        </div>
      )}

      {generated && prospects.length === 0 && (
        <div className="text-center text-base-content/40 py-16">
          <CheckCircleIcon className="w-10 h-10 mx-auto mb-2 text-green" />
          <p>All prospects recruited or dismissed.</p>
        </div>
      )}
    </div>
  );
}

/* ================================================================== */
/*  Scout Tab                                                          */
/* ================================================================== */

function ScoutTab({
  club,
  scoutResults,
  scoutedRegion,
  onScout,
  onSign,
}: {
  club: Club;
  scoutResults: Array<{ player: Player; signFee: number }>;
  scoutedRegion: string | null;
  onScout: (region: ScoutRegion) => void;
  onSign: (player: Player, signFee: number) => void;
}) {
  const regions = getRegions();

  return (
    <div>
      {/* Region list */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mb-6">
        {regions.map((region) => {
          const canAfford = club.balance >= region.cost;
          const isActive = scoutedRegion === region.name;
          return (
            <div
              key={region.name}
              className={`card bg-white shadow-sm border-2 transition-all ${
                isActive ? "border-purple-400 bg-purple-50" : "border-base-200"
              }`}
            >
              <div className="card-body p-4 flex-row items-center justify-between">
                <div className="flex items-center gap-3">
                  <Flag country={region.name} size="lg" />
                  <div>
                    <p className="font-bold">{region.name}</p>
                    <p className="text-xs text-base-content/50">
                      Cost: {formatMoney(region.cost)}
                    </p>
                  </div>
                </div>
                <button
                  className="btn btn-sm btn-outline btn-secondary"
                  disabled={!canAfford}
                  onClick={() => onScout(region)}
                >
                  Scout
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* Scout results */}
      {scoutedRegion && scoutResults.length > 0 && (
        <div>
          <p className="text-sm text-base-content/50 font-medium mb-3">
            Scouted players from {scoutedRegion}:
          </p>
          <div className="flex flex-col gap-3">
            {scoutResults.map(({ player, signFee }) => {
              const squadFull = club.team.players.length >= MAX_SQUAD_SIZE;
              const canAfford = signFee === 0 || club.balance >= signFee;
              return (
                <PlayerCard
                  key={player.id}
                  player={player}
                  subtitle={`${player.primaryPosition} | Age ${player.age} | Free Agent`}
                  price={signFee}
                  actionLabel={signFee > 0 ? `Sign (${formatMoney(signFee)})` : "Sign (Free)"}
                  actionColor="btn-secondary"
                  disabled={squadFull || !canAfford}
                  disabledReason={
                    squadFull ? "Squad full" : !canAfford ? "Can't afford" : undefined
                  }
                  onAction={() => onSign(player, signFee)}
                />
              );
            })}
          </div>
        </div>
      )}

      {scoutedRegion && scoutResults.length === 0 && (
        <div className="text-center text-base-content/40 py-8">
          <CheckCircleIcon className="w-10 h-10 mx-auto mb-2 text-green" />
          <p>All scouted players signed. Scout another region!</p>
        </div>
      )}
    </div>
  );
}

/* ================================================================== */
/*  Shared Player Card                                                 */
/* ================================================================== */

function PlayerCard({
  player,
  subtitle,
  price,
  actionLabel,
  actionColor,
  disabled,
  disabledReason,
  onAction,
}: {
  player: Player;
  subtitle: string;
  price: number;
  actionLabel: string;
  actionColor: string;
  disabled: boolean;
  disabledReason?: string;
  onAction: () => void;
}) {
  const ovr =
    player.primaryPosition === Position.GK
      ? goalkeeperOverall(player.attributes)
      : outfieldOverall(player.attributes);

  const a = player.attributes;

  return (
    <div className="card bg-white shadow-sm hover:shadow-md transition-shadow border border-base-200">
      <div className="card-body p-4">
        {/* Top row: OVR + name + price */}
        <div className="flex items-center gap-3">
          <OvrBadge ovr={ovr} size="lg" />
          <div className="flex-1 min-w-0">
            <p className="font-bold text-base truncate">{player.name}</p>
            <p className="text-xs text-base-content/50">{subtitle}</p>
          </div>
          <div className="text-right shrink-0">
            <p className="font-bold text-green-600 tabular-nums">
              {formatMoney(price)}
            </p>
          </div>
        </div>

        {/* Stat chips */}
        <div className="flex flex-wrap gap-x-3 gap-y-0.5 mt-2">
          <StatChip label="PAC" value={Math.round(a.speed)} />
          <StatChip label="SHO" value={Math.round(a.finishing)} />
          <StatChip label="PAS" value={Math.round(a.passing)} />
          <StatChip label="DRI" value={Math.round(a.dribbling)} />
          <StatChip label="DEF" value={Math.round(a.positioning)} />
          <StatChip label="PHY" value={Math.round(a.strength)} />
        </div>

        {/* Action button */}
        <div className="card-actions justify-end mt-2">
          {disabledReason && disabled && (
            <span className="text-xs text-red-400 self-center mr-2">
              {disabledReason}
            </span>
          )}
          <button
            className={`btn btn-sm ${actionColor} font-bold`}
            disabled={disabled}
            onClick={onAction}
          >
            {actionLabel}
          </button>
        </div>
      </div>
    </div>
  );
}
