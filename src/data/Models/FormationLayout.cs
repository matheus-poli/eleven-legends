namespace ElevenLegends.Data.Models;

/// <summary>
/// Maps formation positions to normalized (0–1) pitch coordinates.
/// X: 0=left, 1=right. Y: 0=top (attacking), 1=bottom (GK end).
/// Positions spread wide enough for ~100px cards at 1080p.
/// </summary>
public static class FormationLayout
{
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
        (0.50f, 0.90f), // GK
        (0.10f, 0.72f), // LB
        (0.33f, 0.76f), // CB
        (0.67f, 0.76f), // CB
        (0.90f, 0.72f), // RB
        (0.10f, 0.46f), // LM
        (0.35f, 0.50f), // CM
        (0.65f, 0.50f), // CM
        (0.90f, 0.46f), // RM
        (0.33f, 0.20f), // ST
        (0.67f, 0.20f), // ST
    ];

    // 4-3-3: GK, LB, CB, CB, RB, CM, CM, CAM, LW, ST, RW
    private static readonly (float X, float Y)[] Positions433 =
    [
        (0.50f, 0.90f), // GK
        (0.10f, 0.72f), // LB
        (0.33f, 0.76f), // CB
        (0.67f, 0.76f), // CB
        (0.90f, 0.72f), // RB
        (0.28f, 0.50f), // CM
        (0.72f, 0.50f), // CM
        (0.50f, 0.40f), // CAM
        (0.10f, 0.22f), // LW
        (0.50f, 0.12f), // ST
        (0.90f, 0.22f), // RW
    ];

    // 3-5-2: GK, CB, CB, CB, LM, CDM, CM, CAM, RM, ST, ST
    private static readonly (float X, float Y)[] Positions352 =
    [
        (0.50f, 0.90f), // GK
        (0.22f, 0.74f), // CB
        (0.50f, 0.78f), // CB
        (0.78f, 0.74f), // CB
        (0.07f, 0.46f), // LM
        (0.50f, 0.58f), // CDM
        (0.28f, 0.44f), // CM
        (0.50f, 0.32f), // CAM
        (0.93f, 0.46f), // RM
        (0.33f, 0.14f), // ST
        (0.67f, 0.14f), // ST
    ];

    // 4-2-3-1: GK, LB, CB, CB, RB, CDM, CDM, LW, CAM, RW, ST
    private static readonly (float X, float Y)[] Positions4231 =
    [
        (0.50f, 0.90f), // GK
        (0.10f, 0.72f), // LB
        (0.33f, 0.76f), // CB
        (0.67f, 0.76f), // CB
        (0.90f, 0.72f), // RB
        (0.33f, 0.56f), // CDM
        (0.67f, 0.56f), // CDM
        (0.10f, 0.32f), // LW
        (0.50f, 0.36f), // CAM
        (0.90f, 0.32f), // RW
        (0.50f, 0.12f), // ST
    ];

    // 5-3-2: GK, LWB, CB, CB, CB, RWB, CM, CM, CAM, ST, ST
    private static readonly (float X, float Y)[] Positions532 =
    [
        (0.50f, 0.90f), // GK
        (0.07f, 0.64f), // LWB
        (0.28f, 0.76f), // CB
        (0.50f, 0.78f), // CB
        (0.72f, 0.76f), // CB
        (0.93f, 0.64f), // RWB
        (0.28f, 0.46f), // CM
        (0.72f, 0.46f), // CM
        (0.50f, 0.34f), // CAM
        (0.33f, 0.14f), // ST
        (0.67f, 0.14f), // ST
    ];
}
