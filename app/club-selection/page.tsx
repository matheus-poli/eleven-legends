"use client";

import { useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { generate } from "@/engine/generators/team-generator";
import { type Club } from "@/engine/models/club";
import { averageOverall } from "@/engine/simulation/formation-optimizer";
import { F442 } from "@/engine/models/formation";
import { OvrBadge, Flag, Money, formatMoney, PageWrapper } from "@/components/ui";
import { useGameStore } from "@/store/game-store";

const COUNTRIES = ["Brasil", "Espa\u00f1a", "England", "Italia"] as const;

export default function ClubSelectionPage() {
  const router = useRouter();
  const store = useGameStore();

  const [selectedCountry, setSelectedCountry] = useState<string>(COUNTRIES[0]);

  // Generate clubs once on mount with a stable seed
  const { clubs, seed } = useMemo(() => {
    const s = Date.now();
    return { clubs: generate(s), seed: s };
  }, []);

  // Group clubs by country
  const clubsByCountry = useMemo(() => {
    const grouped: Record<string, Club[]> = {};
    for (const country of COUNTRIES) {
      grouped[country] = clubs.filter((c) => c.country === country);
    }
    return grouped;
  }, [clubs]);

  const visibleClubs = clubsByCountry[selectedCountry] ?? [];

  function handleManage(club: Club) {
    store.startGame(clubs, club.id, seed);
    router.push("/hub");
  }

  return (
    <PageWrapper gradient="bg-gradient-to-b from-base-200 to-base-300">
      <div className="max-w-5xl mx-auto px-4 py-8">
        {/* Back link */}
        <Link
          href="/"
          className="btn btn-ghost btn-sm gap-1 mb-4 text-base-content/60 hover:text-base-content"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back
        </Link>

        {/* Header card */}
        <div className="card bg-base-100 shadow-xl mb-8">
          <div className="card-body text-center py-8">
            <h1 className="text-4xl font-black tracking-tight text-base-content">
              Choose Your Club
            </h1>
            <p className="text-base-content/60 text-lg mt-1">
              Select a club to begin your career
            </p>
          </div>
        </div>

        {/* Country tabs */}
        <div className="flex justify-center mb-8">
          <div role="tablist" className="tabs tabs-boxed bg-base-100 shadow-md p-1 gap-1">
            {COUNTRIES.map((country) => (
              <button
                key={country}
                role="tab"
                className={`tab tab-lg font-semibold gap-2 transition-all duration-200 ${
                  selectedCountry === country
                    ? "tab-active bg-green text-white shadow-md"
                    : "hover:bg-base-200"
                }`}
                onClick={() => setSelectedCountry(country)}
              >
                <Flag country={country} size="sm" />
                {country}
              </button>
            ))}
          </div>
        </div>

        {/* Club grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {visibleClubs.map((club) => {
            const ovr = averageOverall(club.team.players, F442);

            return (
              <div
                key={club.id}
                className="card card-3d bg-base-100 shadow-lg border-2 border-base-200 hover:border-green transition-all duration-300"
              >
                <div className="card-body p-5 gap-3">
                  {/* OVR + Flag row */}
                  <div className="flex items-center justify-between">
                    <OvrBadge ovr={ovr} size="lg" />
                    <Flag country={club.country} size="lg" />
                  </div>

                  {/* Club name */}
                  <h2 className="card-title text-lg font-bold leading-tight">
                    {club.name}
                  </h2>

                  {/* Stats */}
                  <div className="flex flex-col gap-1.5">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-base-content/50">Rep</span>
                      <div className="flex items-center gap-1.5">
                        <progress
                          className="progress progress-info w-16 h-2"
                          value={club.reputation}
                          max={100}
                        />
                        <span className="font-semibold tabular-nums text-blue w-6 text-right">
                          {club.reputation}
                        </span>
                      </div>
                    </div>
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-base-content/50">Squad</span>
                      <span className="font-semibold tabular-nums">
                        {club.team.players.length} players
                      </span>
                    </div>
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-base-content/50">Budget</span>
                      <Money amount={club.balance} />
                    </div>
                  </div>

                  {/* Manage button */}
                  <div className="card-actions mt-2">
                    <button
                      className="btn btn-primary btn-block shadow-md font-bold btn-raised"
                      onClick={() => handleManage(club)}
                    >
                      Manage
                    </button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </PageWrapper>
  );
}
