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

    /// <summary>
    /// Returns a position-weighted overall rating. Each position emphasizes
    /// different attributes so a CB rates differently than a ST.
    /// </summary>
    public float OverallForPosition(Enums.Position pos)
    {
        if (pos == Enums.Position.GK)
            return GoalkeeperOverall;

        // Weighted average: key attrs x2, secondary x1.5, rest x1
        return pos switch
        {
            Enums.Position.CB => Weighted(
                (Positioning, 2), (Anticipation, 2), (Strength, 2), (Aerial, 2),
                (Composure, 1.5f), (Leadership, 1.5f), (Speed, 1), (Passing, 1),
                (Decisions, 1), (Stamina, 1)),

            Enums.Position.LB or Enums.Position.RB =>
                Weighted(
                (Speed, 2), (Acceleration, 2), (Positioning, 1.5f), (Anticipation, 1.5f),
                (Stamina, 1.5f), (Dribbling, 1), (Passing, 1.5f), (Strength, 1),
                (Composure, 1), (Agility, 1)),

            Enums.Position.LWB or Enums.Position.RWB =>
                Weighted(
                (Speed, 2), (Stamina, 2), (Acceleration, 1.5f), (Dribbling, 1.5f),
                (Passing, 1.5f), (Positioning, 1), (Anticipation, 1), (Technique, 1),
                (Strength, 1), (Agility, 1.5f)),

            Enums.Position.CDM => Weighted(
                (Positioning, 2), (Anticipation, 2), (Strength, 2), (Decisions, 1.5f),
                (Passing, 1.5f), (Composure, 1.5f), (Stamina, 1), (Leadership, 1),
                (Speed, 1), (FirstTouch, 1)),

            Enums.Position.CM => Weighted(
                (Passing, 2), (Decisions, 2), (Composure, 1.5f), (FirstTouch, 1.5f),
                (Stamina, 1.5f), (Positioning, 1), (Technique, 1), (OffTheBall, 1),
                (Dribbling, 1), (Anticipation, 1)),

            Enums.Position.CAM => Weighted(
                (Passing, 2), (Technique, 2), (Decisions, 1.5f), (FirstTouch, 1.5f),
                (Flair, 1.5f), (Composure, 1.5f), (Dribbling, 1), (OffTheBall, 1),
                (Finishing, 1), (Acceleration, 1)),

            Enums.Position.LM or Enums.Position.RM =>
                Weighted(
                (Speed, 2), (Dribbling, 2), (Stamina, 1.5f), (Passing, 1.5f),
                (Acceleration, 1.5f), (Technique, 1), (FirstTouch, 1), (Agility, 1),
                (OffTheBall, 1), (Composure, 1)),

            Enums.Position.LW or Enums.Position.RW =>
                Weighted(
                (Speed, 2), (Dribbling, 2), (Acceleration, 2), (Technique, 1.5f),
                (Flair, 1.5f), (Finishing, 1), (FirstTouch, 1), (Agility, 1),
                (OffTheBall, 1), (Composure, 1)),

            Enums.Position.CF => Weighted(
                (Finishing, 2), (Composure, 2), (FirstTouch, 1.5f), (Technique, 1.5f),
                (OffTheBall, 1.5f), (Decisions, 1), (Dribbling, 1), (Passing, 1),
                (Flair, 1), (Strength, 1)),

            Enums.Position.ST => Weighted(
                (Finishing, 2), (OffTheBall, 2), (Composure, 1.5f), (Speed, 1.5f),
                (Strength, 1.5f), (FirstTouch, 1), (Dribbling, 1), (Anticipation, 1),
                (Agility, 1), (BigMatches, 1)),

            _ => OutfieldOverall,
        };
    }

    private static float Weighted(params (int attr, float weight)[] pairs)
    {
        float sum = 0f;
        float totalWeight = 0f;
        foreach ((int attr, float weight) in pairs)
        {
            sum += attr * weight;
            totalWeight += weight;
        }
        return sum / totalWeight;
    }
}
