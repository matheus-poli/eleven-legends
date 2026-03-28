namespace ElevenLegends.Data.Models;

/// <summary>
/// Maps formation positions to normalized (0–1) pitch coordinates.
/// X: 0=left, 1=right. Y: 0=top (attacking), 1=bottom (GK end).
/// </summary>
public static class FormationLayout
{
    /// <summary>
    /// Returns (x, y) coordinates for each slot in the given formation.
    /// Coordinates are normalized 0–1 where (0.5, 1.0) is the GK position.
    /// </summary>
    public static IReadOnlyList<(float X, float Y)> GetPositions(Formation formation)
    {
        if (formation == Formation.F442) return Positions442;
        if (formation == Formation.F433) return Positions433;
        if (formation == Formation.F352) return Positions352;
        if (formation == Formation.F4231) return Positions4231;
        if (formation == Formation.F532) return Positions532;
        return Positions442;
    }

    // 4-4-2: GK, LB, CB, CB, RB, LM, CM, CM, RM, ST, ST
    private static readonly (float X, float Y)[] Positions442 =
    [
        (0.50f, 0.92f), // GK
        (0.12f, 0.74f), // LB
        (0.37f, 0.78f), // CB
        (0.63f, 0.78f), // CB
        (0.88f, 0.74f), // RB
        (0.12f, 0.48f), // LM
        (0.37f, 0.52f), // CM
        (0.63f, 0.52f), // CM
        (0.88f, 0.48f), // RM
        (0.35f, 0.20f), // ST
        (0.65f, 0.20f), // ST
    ];

    // 4-3-3: GK, LB, CB, CB, RB, CM, CM, CAM, LW, ST, RW
    private static readonly (float X, float Y)[] Positions433 =
    [
        (0.50f, 0.92f), // GK
        (0.12f, 0.74f), // LB
        (0.37f, 0.78f), // CB
        (0.63f, 0.78f), // CB
        (0.88f, 0.74f), // RB
        (0.30f, 0.52f), // CM
        (0.70f, 0.52f), // CM
        (0.50f, 0.42f), // CAM
        (0.12f, 0.22f), // LW
        (0.50f, 0.14f), // ST
        (0.88f, 0.22f), // RW
    ];

    // 3-5-2: GK, CB, CB, CB, LM, CDM, CM, CAM, RM, ST, ST
    private static readonly (float X, float Y)[] Positions352 =
    [
        (0.50f, 0.92f), // GK
        (0.25f, 0.76f), // CB
        (0.50f, 0.80f), // CB
        (0.75f, 0.76f), // CB
        (0.08f, 0.48f), // LM
        (0.50f, 0.60f), // CDM
        (0.30f, 0.46f), // CM
        (0.50f, 0.34f), // CAM
        (0.92f, 0.48f), // RM
        (0.35f, 0.16f), // ST
        (0.65f, 0.16f), // ST
    ];

    // 4-2-3-1: GK, LB, CB, CB, RB, CDM, CDM, LW, CAM, RW, ST
    private static readonly (float X, float Y)[] Positions4231 =
    [
        (0.50f, 0.92f), // GK
        (0.12f, 0.74f), // LB
        (0.37f, 0.78f), // CB
        (0.63f, 0.78f), // CB
        (0.88f, 0.74f), // RB
        (0.35f, 0.58f), // CDM
        (0.65f, 0.58f), // CDM
        (0.12f, 0.34f), // LW
        (0.50f, 0.38f), // CAM
        (0.88f, 0.34f), // RW
        (0.50f, 0.14f), // ST
    ];

    // 5-3-2: GK, LWB, CB, CB, CB, RWB, CM, CM, CAM, ST, ST
    private static readonly (float X, float Y)[] Positions532 =
    [
        (0.50f, 0.92f), // GK
        (0.08f, 0.66f), // LWB
        (0.30f, 0.78f), // CB
        (0.50f, 0.80f), // CB
        (0.70f, 0.78f), // CB
        (0.92f, 0.66f), // RWB
        (0.30f, 0.48f), // CM
        (0.70f, 0.48f), // CM
        (0.50f, 0.36f), // CAM
        (0.35f, 0.16f), // ST
        (0.65f, 0.16f), // ST
    ];
}
