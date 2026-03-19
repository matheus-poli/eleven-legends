using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Economy;

namespace ElevenLegends.Manager;

/// <summary>
/// Manages the coach's career: reputation, dismissal, win condition.
/// </summary>
public static class CareerManager
{
    /// <summary>Reputation gain per phase survived.</summary>
    private const int ReputationPerPhase = 5;

    /// <summary>Reputation bonus for winning a title.</summary>
    private const int ReputationForTitle = 20;

    /// <summary>Reputation loss for early elimination.</summary>
    private const int ReputationLossElimination = 5;

    /// <summary>
    /// Updates reputation after a competition phase result.
    /// </summary>
    public static void UpdateReputation(ManagerState manager, CompetitionPhase phase, bool advanced)
    {
        if (advanced)
        {
            manager.Reputation = Math.Min(100, manager.Reputation + ReputationPerPhase);

            if (phase == CompetitionPhase.Final || phase == CompetitionPhase.MundialFinal)
                manager.Reputation = Math.Min(100, manager.Reputation + ReputationForTitle);
        }
        else
        {
            manager.Reputation = Math.Max(0, manager.Reputation - ReputationLossElimination);
        }
    }

    /// <summary>
    /// Pays the manager's monthly salary. Call once per week for simplicity.
    /// </summary>
    public static void PaySalary(ManagerState manager)
    {
        manager.PersonalBalance += manager.Salary;
    }

    /// <summary>
    /// Checks if the manager should be dismissed (club bankrupt).
    /// In the demo, dismissal is instant game over.
    /// </summary>
    public static void CheckDismissal(ManagerState manager, Club club)
    {
        if (EconomyProcessor.IsBankrupt(club))
        {
            manager.Status = ManagerStatus.Dismissed;
        }
    }

    /// <summary>
    /// Checks if the manager won the mundial — triggers the win screen.
    /// </summary>
    public static void CheckVictory(ManagerState manager, int? mundialChampionId)
    {
        if (mundialChampionId.HasValue && mundialChampionId.Value == manager.ClubId)
        {
            manager.Status = ManagerStatus.Winner;
        }
    }

    /// <summary>
    /// In the demo, dismissal = game over directly.
    /// </summary>
    public static bool IsGameOver(ManagerState manager) =>
        manager.Status is ManagerStatus.Dismissed or ManagerStatus.GameOver;

    /// <summary>
    /// Returns true if the manager won the mundial.
    /// </summary>
    public static bool IsVictory(ManagerState manager) =>
        manager.Status == ManagerStatus.Winner;
}
