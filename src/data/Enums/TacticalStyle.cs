namespace ElevenLegends.Data.Enums;

/// <summary>
/// Tactical style affecting simulation weights.
/// </summary>
public enum TacticalStyle
{
    /// <summary>More aggressive possession, higher shot attempts.</summary>
    Attacking,

    /// <summary>Default balanced approach.</summary>
    Balanced,

    /// <summary>Sit deeper, prioritize defense and counter-attacks.</summary>
    Defensive
}
