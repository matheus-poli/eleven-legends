"use client";

import { useRouter } from "next/navigation";
import { useGameStore } from "@/store/game-store";
import { PageWrapper, Money, formatMoney } from "@/components/ui";
import { isGameOver, isVictory } from "@/engine/career-manager";
import { ManagerStatus } from "@/engine/enums/manager-status";
import { TransferType } from "@/engine/enums/transfer-type";

/* ================================================================== */
/*  Season End / Game Over Page                                        */
/* ================================================================== */

export default function SeasonEndPage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.gameState);
  const clearGame = useGameStore((s) => s.clearGame);

  if (!gameState) {
    router.replace("/");
    return null;
  }

  const gs = gameState;
  const manager = gs.manager;
  const club = gs.playerClub;

  const gameOver = isGameOver(manager);
  const victory = isVictory(manager);

  /* ---- Determine variant ----------------------------------------- */

  type Variant = "champion" | "dismissed" | "season";

  const variant: Variant = victory
    ? "champion"
    : gameOver
      ? "dismissed"
      : "season";

  const variantConfig: Record<
    Variant,
    { gradient: string; icon: string; title: string; subtitle: string }
  > = {
    champion: {
      gradient: "bg-gradient-to-b from-yellow-400 to-yellow-600",
      icon: "🏆",
      title: "CHAMPION!",
      subtitle: club.name,
    },
    dismissed: {
      gradient: "bg-gradient-to-b from-gray-500 to-gray-700",
      icon: "💼",
      title: "Game Over",
      subtitle: "You have been sacked.",
    },
    season: {
      gradient: "bg-gradient-to-b from-blue-500 to-blue-700",
      icon: "📊",
      title: "Season Complete",
      subtitle: `${club.name} — End of Season`,
    },
  };

  const config = variantConfig[variant];

  /* ---- Transfer summary ------------------------------------------ */

  const myTransfers = gs.transferHistory.filter(
    (t) => t.toClubId === club.id || t.fromClubId === club.id,
  );

  const transferLabel: Record<string, string> = {
    [TransferType.Buy]: "Signed",
    [TransferType.Sell]: "Sold",
    [TransferType.LoanIn]: "Loan In",
    [TransferType.LoanOut]: "Loan Out",
    [TransferType.YouthRecruit]: "Youth",
    [TransferType.ScoutRecruit]: "Scouted",
  };

  /* ---- Handlers -------------------------------------------------- */

  function handleMainMenu() {
    clearGame();
    router.push("/");
  }

  /* ================================================================ */
  /*  Render                                                           */
  /* ================================================================ */

  return (
    <PageWrapper gradient={config.gradient}>
      <div className="flex flex-col items-center min-h-screen px-4 py-10">
        {/* ---- Hero header ---- */}
        <div className="text-center mb-8 mt-6">
          <span
            className={`text-7xl block mb-4 ${
              variant === "champion" ? "animate-bounce" : ""
            }`}
          >
            {config.icon}
          </span>
          <h1
            className={`text-4xl font-black tracking-tight ${
              variant === "dismissed" ? "text-white/80" : "text-white"
            }`}
          >
            {config.title}
          </h1>
          <p className="text-white/70 text-lg mt-2">{config.subtitle}</p>
        </div>

        {/* ---- Stats card ---- */}
        <div className="card bg-white shadow-xl w-full max-w-md mb-4">
          <div className="card-body p-5">
            <h2 className="card-title text-sm text-base-content/50 uppercase tracking-widest mb-3">
              Season Summary
            </h2>

            <StatRow label="Club" value={club.name} />
            <StatRow
              label="Reputation"
              value={
                <div className="flex items-center gap-2">
                  <progress
                    className="progress progress-primary w-20 h-2"
                    value={manager.reputation}
                    max={100}
                  />
                  <span className="font-bold tabular-nums text-sm">
                    {manager.reputation}
                  </span>
                </div>
              }
            />
            <StatRow
              label="Balance"
              value={<Money amount={club.balance} />}
            />
            <StatRow
              label="Squad Size"
              value={
                <span className="font-bold tabular-nums">
                  {club.team.players.length}
                </span>
              }
            />
            <StatRow
              label="Status"
              value={
                <StatusBadge status={manager.status} />
              }
            />
          </div>
        </div>

        {/* ---- Transfer Activity card ---- */}
        {myTransfers.length > 0 && (
          <div className="card bg-white shadow-xl w-full max-w-md mb-6">
            <div className="card-body p-5">
              <h2 className="card-title text-sm text-base-content/50 uppercase tracking-widest mb-3">
                Transfer Activity
              </h2>

              <div className="flex flex-col gap-2">
                {myTransfers.map((t, i) => {
                  const isIncoming = t.toClubId === club.id;
                  const icon = isIncoming ? "↙️" : "↗️";
                  const color = isIncoming ? "text-green-600" : "text-red-600";

                  return (
                    <div
                      key={`${t.playerId}-${i}`}
                      className="flex items-center justify-between py-1.5 border-b border-base-200 last:border-0"
                    >
                      <div className="flex items-center gap-2 min-w-0">
                        <span className="text-sm">{icon}</span>
                        <div className="min-w-0">
                          <p className="text-sm font-semibold truncate">
                            {t.playerName}
                          </p>
                          <p className="text-xs text-base-content/40">
                            {transferLabel[t.type] ?? t.type} — Day {t.day + 1}
                          </p>
                        </div>
                      </div>
                      <span className={`text-sm font-bold tabular-nums ${color}`}>
                        {isIncoming ? "-" : "+"}
                        {formatMoney(t.fee)}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        )}

        {/* ---- Main Menu button ---- */}
        <button
          className={`btn btn-lg font-bold shadow-lg w-full max-w-md ${
            variant === "champion"
              ? "btn-warning"
              : variant === "dismissed"
                ? "btn-neutral"
                : "btn-primary"
          }`}
          onClick={handleMainMenu}
        >
          Main Menu
        </button>
      </div>
    </PageWrapper>
  );
}

/* ================================================================== */
/*  Sub-components                                                     */
/* ================================================================== */

function StatRow({
  label,
  value,
}: {
  label: string;
  value: React.ReactNode;
}) {
  return (
    <div className="flex items-center justify-between py-1.5 border-b border-base-200 last:border-0">
      <span className="text-sm text-base-content/50">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

function StatusBadge({ status }: { status: ManagerStatus }) {
  const styles: Record<ManagerStatus, { badge: string; label: string }> = {
    [ManagerStatus.Employed]: { badge: "badge-success", label: "Employed" },
    [ManagerStatus.Dismissed]: { badge: "badge-error", label: "Sacked" },
    [ManagerStatus.Winner]: { badge: "badge-warning", label: "Champion" },
    [ManagerStatus.GameOver]: { badge: "badge-neutral", label: "Game Over" },
  };

  const s = styles[status];
  return <span className={`badge ${s.badge} badge-sm font-bold`}>{s.label}</span>;
}
