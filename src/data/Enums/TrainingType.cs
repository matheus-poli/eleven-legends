namespace ElevenLegends.Data.Enums;

/// <summary>
/// Types of training sessions the manager can choose.
/// </summary>
public enum TrainingType
{
    /// <summary>High intensity — big attribute gains but morale risk.</summary>
    IntenseDrills,

    /// <summary>Team tactics — builds chemistry between players.</summary>
    TacticalSession,

    /// <summary>Light session — small safe gains, good for recovery weeks.</summary>
    LightTraining,

    /// <summary>Full rest day — recovers morale for stressed players.</summary>
    RestDay,

    /// <summary>Youth showcase — reserves get attention, potential breakthroughs.</summary>
    YouthFocus,
}
