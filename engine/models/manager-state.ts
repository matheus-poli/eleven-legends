import { ManagerStatus } from "@/engine/enums";

/** The manager's career state: employment, reputation, finances. */
export interface ManagerState {
  name: string;
  status: ManagerStatus;
  clubId: number;

  /** Manager reputation 0-100. Affects job proposals. */
  reputation: number;

  /** Manager personal balance (salary + bonuses). */
  personalBalance: number;

  /** Monthly salary from current club. */
  salary: number;
}
