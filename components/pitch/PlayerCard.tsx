"use client";

import { type Player } from "@/engine/models/player";
import { type Position } from "@/engine/enums/position";
import { overallForPosition } from "@/engine/models/player-attributes";
import { OvrBadge, RatingBadge } from "@/components/ui/badge";
import { useTilt } from "@/lib/use-tilt";

interface PlayerCardProps {
  player: Player;
  slotPosition?: Position;
  matchRating?: number;
  tint?: "home" | "away" | "neutral";
  showPositionWarning?: boolean;
  onClick?: () => void;
  className?: string;
  size?: "sm" | "md";
  /** Disable 3D tilt (useful for small cards in match view) */
  disableTilt?: boolean;
  /** Make card look draggable */
  draggable?: boolean;
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
  disableTilt = false,
  draggable = false,
}: PlayerCardProps) {
  const { ref, handlers } = useTilt(size === "sm" ? 5 : 10, size === "sm" ? 1.02 : 1.05);
  const pos = slotPosition ?? player.primaryPosition;
  const ovr = overallForPosition(player.attributes, pos);

  const isOutOfPosition = showPositionWarning &&
    slotPosition !== undefined &&
    player.primaryPosition !== slotPosition &&
    player.secondaryPosition !== slotPosition;

  const tintBg =
    tint === "home" ? "bg-gradient-to-b from-blue-50 to-blue-100/80 border-blue-200" :
    tint === "away" ? "bg-gradient-to-b from-red-50 to-red-100/80 border-red-200" :
    "bg-gradient-to-b from-white to-base-100 border-base-300";

  const sizeClasses = size === "sm"
    ? "w-[76px] p-1.5 gap-0.5"
    : "w-[104px] p-2.5 gap-1";

  const nameSize = size === "sm" ? "text-[11px]" : "text-sm";
  const posSize = size === "sm" ? "text-[9px]" : "text-xs";

  const tiltHandlers = disableTilt ? {} : handlers;

  return (
    <div
      ref={disableTilt ? undefined : ref}
      {...tiltHandlers}
      className={`card-3d flex flex-col items-center rounded-xl border-2 shadow-md select-none
        ${isOutOfPosition ? "border-red! bg-gradient-to-b from-red-50 to-red-100" : tintBg}
        ${onClick ? "cursor-pointer active:scale-95" : ""}
        ${draggable ? "cursor-grab active:cursor-grabbing" : ""}
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
      <span className={`${nameSize} font-semibold text-base-content text-center leading-tight truncate w-full`}>
        {player.name}
      </span>

      {/* Position */}
      <span className={`${posSize} font-medium ${isOutOfPosition ? "text-red font-bold" : "text-base-content/40"}`}>
        {isOutOfPosition ? `${player.primaryPosition}→${slotPosition}` : pos}
      </span>
    </div>
  );
}
