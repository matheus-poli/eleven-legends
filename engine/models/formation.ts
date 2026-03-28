import { Position } from "@/engine/enums";

/** A formation defines the 11 positions for the starting lineup. */
export interface Formation {
  name: string;
  positions: readonly Position[];
}

export const F442: Formation = {
  name: "4-4-2",
  positions: [
    Position.GK,
    Position.LB,
    Position.CB,
    Position.CB,
    Position.RB,
    Position.LM,
    Position.CM,
    Position.CM,
    Position.RM,
    Position.ST,
    Position.ST,
  ],
} as const;

export const F433: Formation = {
  name: "4-3-3",
  positions: [
    Position.GK,
    Position.LB,
    Position.CB,
    Position.CB,
    Position.RB,
    Position.CM,
    Position.CM,
    Position.CAM,
    Position.LW,
    Position.ST,
    Position.RW,
  ],
} as const;

export const F352: Formation = {
  name: "3-5-2",
  positions: [
    Position.GK,
    Position.CB,
    Position.CB,
    Position.CB,
    Position.LM,
    Position.CDM,
    Position.CM,
    Position.CAM,
    Position.RM,
    Position.ST,
    Position.ST,
  ],
} as const;

export const F4231: Formation = {
  name: "4-2-3-1",
  positions: [
    Position.GK,
    Position.LB,
    Position.CB,
    Position.CB,
    Position.RB,
    Position.CDM,
    Position.CDM,
    Position.LW,
    Position.CAM,
    Position.RW,
    Position.ST,
  ],
} as const;

export const F532: Formation = {
  name: "5-3-2",
  positions: [
    Position.GK,
    Position.LWB,
    Position.CB,
    Position.CB,
    Position.CB,
    Position.RWB,
    Position.CM,
    Position.CM,
    Position.CAM,
    Position.ST,
    Position.ST,
  ],
} as const;

export const FORMATION_PRESETS: readonly Formation[] = [
  F442,
  F433,
  F352,
  F4231,
  F532,
];
