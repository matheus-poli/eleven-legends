using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// A formation defines the 11 positions for the starting lineup.
/// </summary>
public sealed record Formation
{
    public required string Name { get; init; }
    public required IReadOnlyList<Position> Positions { get; init; }

    public static readonly Formation F442 = new()
    {
        Name = "4-4-2",
        Positions = [Position.GK, Position.LB, Position.CB, Position.CB, Position.RB,
                     Position.LM, Position.CM, Position.CM, Position.RM,
                     Position.ST, Position.ST]
    };

    public static readonly Formation F433 = new()
    {
        Name = "4-3-3",
        Positions = [Position.GK, Position.LB, Position.CB, Position.CB, Position.RB,
                     Position.CM, Position.CM, Position.CAM,
                     Position.LW, Position.ST, Position.RW]
    };

    public static readonly Formation F352 = new()
    {
        Name = "3-5-2",
        Positions = [Position.GK, Position.CB, Position.CB, Position.CB,
                     Position.LM, Position.CDM, Position.CM, Position.CAM, Position.RM,
                     Position.ST, Position.ST]
    };

    public static readonly Formation F4231 = new()
    {
        Name = "4-2-3-1",
        Positions = [Position.GK, Position.LB, Position.CB, Position.CB, Position.RB,
                     Position.CDM, Position.CDM,
                     Position.LW, Position.CAM, Position.RW,
                     Position.ST]
    };

    public static readonly Formation F532 = new()
    {
        Name = "5-3-2",
        Positions = [Position.GK, Position.LWB, Position.CB, Position.CB, Position.CB, Position.RWB,
                     Position.CM, Position.CM, Position.CAM,
                     Position.ST, Position.ST]
    };

    public static readonly IReadOnlyList<Formation> Presets = [F442, F433, F352, F4231, F532];
}
