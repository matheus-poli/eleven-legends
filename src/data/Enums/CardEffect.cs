namespace ElevenLegends.Data.Enums;

/// <summary>
/// Types of locker room card effects applied at halftime.
/// </summary>
public enum CardEffect
{
    /// <summary>Boosts morale for the team or a specific player.</summary>
    MoraleBoost,

    /// <summary>Recovers stamina for fatigued players.</summary>
    StaminaRecovery,

    /// <summary>Temporary buff to team attributes in the second half.</summary>
    TeamBuff,

    /// <summary>Reduces opponent morale or performance.</summary>
    OpponentDebuff
}
