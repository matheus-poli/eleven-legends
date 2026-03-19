namespace ElevenLegends.Data.Models;

/// <summary>
/// All player attributes on a 0–100 scale.
/// </summary>
public sealed record PlayerAttributes
{
    // Technical
    public int Finishing { get; init; }
    public int Passing { get; init; }
    public int Dribbling { get; init; }
    public int FirstTouch { get; init; }
    public int Technique { get; init; }

    // Mental
    public int Decisions { get; init; }
    public int Composure { get; init; }
    public int Positioning { get; init; }
    public int Anticipation { get; init; }
    public int OffTheBall { get; init; }

    // Physical
    public int Speed { get; init; }
    public int Acceleration { get; init; }
    public int Stamina { get; init; }
    public int Strength { get; init; }
    public int Agility { get; init; }

    // Special
    public int Consistency { get; init; }
    public int Leadership { get; init; }
    public int Flair { get; init; }
    public int BigMatches { get; init; }

    // Goalkeeper-exclusive
    public int Reflexes { get; init; }
    public int Handling { get; init; }
    public int GkPositioning { get; init; }
    public int Aerial { get; init; }

    /// <summary>
    /// Returns the overall average of outfield attributes (excludes GK-specific).
    /// </summary>
    public float OutfieldOverall =>
        (Finishing + Passing + Dribbling + FirstTouch + Technique +
         Decisions + Composure + Positioning + Anticipation + OffTheBall +
         Speed + Acceleration + Stamina + Strength + Agility +
         Consistency + Leadership + Flair + BigMatches) / 19f;

    /// <summary>
    /// Returns the overall average of goalkeeper attributes.
    /// </summary>
    public float GoalkeeperOverall =>
        (Reflexes + Handling + GkPositioning + Aerial +
         Composure + Decisions + Positioning + Leadership + Consistency) / 9f;
}
