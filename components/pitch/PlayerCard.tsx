"use client";

import { type Player } from "@/engine/models/player";
import { type Position } from "@/engine/enums/position";
import { overallForPosition } from "@/engine/models/player-attributes";
import { OvrBadge, RatingBadge } from "@/components/ui/badge";

interface PlayerCardProps {
  player: Player;
  /** The slot position this card occupies (may differ from player's natural position) */
  slotPosition?: Position;
  /** Match rating (0-10). If provided, shows rating instead of OVR. */
  matchRating?: number;
  /** Team tint — "home" or "away" */
  tint?: "home" | "away" | "neutral";
  /** Show warning if out of position */
  showPositionWarning?: boolean;
  onClick?: () => void;
  className?: string;
  size?: "sm" | "md";
}

export function PlayerCard({
  player,
  slotPosition,
  matchRating,
  tint = "neutral",
  showPositionWarning = true,
  onClick,
  className = "",
  size = "md",
}: PlayerCardProps) {
  const pos = slotPosition ?? player.primaryPosition;
  const ovr = overallForPosition(player.attributes, pos);

  const isOutOfPosition = showPositionWarning &&
    slotPosition !== undefined &&
    player.primaryPosition !== slotPosition &&
    player.secondaryPosition !== slotPosition;

  const tintBg =
    tint === "home" ? "bg-blue-50 border-blue-200" :
    tint === "away" ? "bg-red-50 border-red-200" :
    "bg-white border-base-300";

  const sizeClasses = size === "sm"
    ? "w-[72px] p-1.5 gap-0.5"
    : "w-[100px] p-2 gap-1";

  const nameSize = size === "sm" ? "text-[11px]" : "text-xs";
  const posSize = size === "sm" ? "text-[9px]" : "text-[11px]";

  return (
    <div
      className={`card-3d flex flex-col items-center rounded-xl border-2 shadow-md
        ${isOutOfPosition ? "border-red! bg-red-50" : tintBg}
        ${onClick ? "cursor-pointer" : ""}
        ${sizeClasses} ${className}`}
      onClick={onClick}
    >
      {/* Rating or OVR badge */}
      {matchRating !== undefined ? (
        <RatingBadge rating={matchRating} size={size === "sm" ? "xs" : "sm"} />
      ) : (
        <OvrBadge ovr={ovr} size={size === "sm" ? "xs" : "sm"} />
      )}

      {/* Player name */}
      <span className={`${nameSize} font-medium text-base-content text-center leading-tight truncate w-full`}>
        {player.name}
      </span>

      {/* Position */}
      <span className={`${posSize} ${isOutOfPosition ? "text-red font-bold" : "text-base-content/50"}`}>
        {isOutOfPosition ? `${player.primaryPosition}→${slotPosition}` : pos}
      </span>
    </div>
  );
}
