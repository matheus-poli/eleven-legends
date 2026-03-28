using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes.Components;

/// <summary>
/// Football pitch component that renders a green field with player cards
/// positioned according to the current formation. Supports drag-and-drop.
/// </summary>
public partial class PitchView : Control
{
    // ─── Pitch drawing colors ────────────────────────────────────────
    private static readonly Color PitchGreen = new("2E7D32");
    private static readonly Color PitchGreenLight = new("388E3C");
    private static readonly Color LineWhite = new("FFFFFF40");

    private Formation _formation = Formation.F442;
    private int[] _slotPlayerIds = new int[11];
    private IReadOnlyList<Player> _squad = [];
    private Dictionary<int, Player> _playerMap = new();

    // UI nodes for each slot
    private readonly Control[] _slotNodes = new Control[11];

    [Signal]
    public delegate void LineupChangedEventHandler();

    [Signal]
    public delegate void PlayerClickedEventHandler(int playerId);

    public Formation CurrentFormation => _formation;
    public IReadOnlyList<int> SlotPlayerIds => _slotPlayerIds;

    public void Setup(IReadOnlyList<Player> squad, Formation formation, IReadOnlyList<int> startingIds)
    {
        _squad = squad;
        _playerMap = squad.ToDictionary(p => p.Id);
        _formation = formation;

        _slotPlayerIds = new int[formation.Positions.Count];
        for (int i = 0; i < Math.Min(startingIds.Count, _slotPlayerIds.Length); i++)
        {
            _slotPlayerIds[i] = startingIds[i];
        }

        RebuildCards();
    }

    public void SetFormation(Formation formation, IReadOnlyList<int> playerIds)
    {
        _formation = formation;
        _slotPlayerIds = new int[formation.Positions.Count];
        for (int i = 0; i < Math.Min(playerIds.Count, _slotPlayerIds.Length); i++)
        {
            _slotPlayerIds[i] = playerIds[i];
        }
        RebuildCards();
        EmitSignal(SignalName.LineupChanged);
    }

    /// <summary>
    /// Assigns a player to a slot. If the player was in another slot, swaps.
    /// </summary>
    public void AssignPlayer(int playerId, int slotIndex)
    {
        // Remove player from any existing slot
        for (int i = 0; i < _slotPlayerIds.Length; i++)
        {
            if (_slotPlayerIds[i] == playerId && i != slotIndex)
            {
                // Swap with current occupant
                _slotPlayerIds[i] = _slotPlayerIds[slotIndex];
                break;
            }
        }

        _slotPlayerIds[slotIndex] = playerId;
        RebuildCards();
        EmitSignal(SignalName.LineupChanged);
    }

    /// <summary>
    /// Returns IDs of players not in any slot (bench players).
    /// </summary>
    public IReadOnlyList<Player> GetBenchPlayers()
    {
        HashSet<int> onPitch = new(_slotPlayerIds.Where(id => id > 0));
        return _squad.Where(p => !onPitch.Contains(p.Id)).ToList();
    }

    public override void _Ready()
    {
        ClipContents = true;
    }

    public override void _Draw()
    {
        DrawPitch();
    }

    private void DrawPitch()
    {
        Vector2 size = Size;
        float w = size.X;
        float h = size.Y;
        float lineWidth = 1.5f;

        // Main pitch rectangle
        var pitchRect = new Rect2(0, 0, w, h);
        DrawRect(pitchRect, PitchGreen);

        // Stripes (alternating slightly lighter green)
        int stripes = 10;
        float stripeH = h / stripes;
        for (int i = 0; i < stripes; i += 2)
        {
            DrawRect(new Rect2(0, i * stripeH, w, stripeH), PitchGreenLight);
        }

        // Outer border
        DrawRect(pitchRect, LineWhite, false, lineWidth);

        // Center line
        DrawLine(new Vector2(0, h / 2), new Vector2(w, h / 2), LineWhite, lineWidth);

        // Center circle
        DrawArc(new Vector2(w / 2, h / 2), h * 0.1f, 0, Mathf.Tau, 48, LineWhite, lineWidth);

        // Center dot
        DrawCircle(new Vector2(w / 2, h / 2), 3, LineWhite);

        // Penalty box top (attacking end)
        float boxW = w * 0.44f;
        float boxH = h * 0.14f;
        DrawRect(new Rect2((w - boxW) / 2, 0, boxW, boxH), LineWhite, false, lineWidth);

        // Small box top
        float sboxW = w * 0.2f;
        float sboxH = h * 0.06f;
        DrawRect(new Rect2((w - sboxW) / 2, 0, sboxW, sboxH), LineWhite, false, lineWidth);

        // Penalty box bottom (GK end)
        DrawRect(new Rect2((w - boxW) / 2, h - boxH, boxW, boxH), LineWhite, false, lineWidth);

        // Small box bottom
        DrawRect(new Rect2((w - sboxW) / 2, h - sboxH, sboxW, sboxH), LineWhite, false, lineWidth);

        // Corner arcs
        float cornerR = h * 0.025f;
        DrawArc(new Vector2(0, 0), cornerR, 0, Mathf.Pi / 2, 12, LineWhite, lineWidth);
        DrawArc(new Vector2(w, 0), cornerR, Mathf.Pi / 2, Mathf.Pi, 12, LineWhite, lineWidth);
        DrawArc(new Vector2(0, h), cornerR, -Mathf.Pi / 2, 0, 12, LineWhite, lineWidth);
        DrawArc(new Vector2(w, h), cornerR, Mathf.Pi, Mathf.Pi * 1.5f, 12, LineWhite, lineWidth);

        // Chemistry lines between adjacent players
        DrawChemistryLines();
    }

    private void DrawChemistryLines()
    {
        IReadOnlyList<Vector2> positions = GetPitchPositions(_formation);
        Vector2 size = Size;

        for (int i = 0; i < _slotPlayerIds.Length; i++)
        {
            if (_slotPlayerIds[i] <= 0 || !_playerMap.ContainsKey(_slotPlayerIds[i])) continue;
            Player p1 = _playerMap[_slotPlayerIds[i]];

            for (int j = i + 1; j < _slotPlayerIds.Length; j++)
            {
                if (_slotPlayerIds[j] <= 0 || !_playerMap.ContainsKey(_slotPlayerIds[j])) continue;
                Player p2 = _playerMap[_slotPlayerIds[j]];

                // Only draw lines between nearby players
                Vector2 pos1 = positions[i] * size;
                Vector2 pos2 = positions[j] * size;
                float dist = pos1.DistanceTo(pos2);
                if (dist > size.Y * 0.35f) continue;

                int avgChem = (p1.Chemistry + p2.Chemistry) / 2;
                Color chemColor;
                if (avgChem >= 70)
                    chemColor = new Color(0.34f, 0.8f, 0.01f, 0.3f); // green
                else if (avgChem >= 40)
                    chemColor = new Color(1f, 0.78f, 0f, 0.2f); // yellow
                else
                    chemColor = new Color(1f, 0.29f, 0.29f, 0.2f); // red

                DrawLine(pos1, pos2, chemColor, 2f);
            }
        }
    }

    private void RebuildCards()
    {
        // Remove old slot nodes
        for (int i = 0; i < _slotNodes.Length; i++)
        {
            _slotNodes[i]?.QueueFree();
            _slotNodes[i] = null!;
        }

        IReadOnlyList<Vector2> positions = GetPitchPositions(_formation);
        Vector2 pitchSize = Size;
        Vector2 cardSize = new(100, 100);

        for (int i = 0; i < _formation.Positions.Count; i++)
        {
            int playerId = i < _slotPlayerIds.Length ? _slotPlayerIds[i] : 0;
            Position slotPos = _formation.Positions[i];
            Vector2 normalizedPos = i < positions.Count ? positions[i] : new Vector2(0.5f, 0.5f);

            Control card = CreateSlotCard(playerId, slotPos, i);
            card.Position = normalizedPos * pitchSize - cardSize / 2;
            card.Size = cardSize;
            AddChild(card);
            _slotNodes[i] = card;
        }

        QueueRedraw();
    }

    private Control CreateSlotCard(int playerId, Position slotPos, int slotIndex)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(100, 100);

        bool hasPlayer = playerId > 0 && _playerMap.ContainsKey(playerId);
        Player? player = hasPlayer ? _playerMap[playerId] : null;

        Color cardBg = hasPlayer ? new Color("FFFFFFEE") : new Color("FFFFFF44");
        bool outOfPosition = hasPlayer && player!.PrimaryPosition != slotPos
            && player.SecondaryPosition != slotPos;

        Color borderColor = outOfPosition ? UITheme.Red : UITheme.Green;
        if (!hasPlayer) borderColor = new Color("FFFFFF66");

        var style = new StyleBoxFlat
        {
            BgColor = cardBg,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
            BorderWidthTop = 3,
            BorderColor = borderColor,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vbox);

        if (hasPlayer)
        {
            float ovr = player!.Attributes.OverallForPosition(slotPos);

            // OVR badge
            var ovrBadge = UITheme.CreateBadge($"{ovr:F0}",
                UITheme.RatingColor(ovr), UITheme.TextDark,
                UITheme.FontSizeBody, new Vector2(40, 30));
            ovrBadge.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            vbox.AddChild(ovrBadge);

            // Name
            string displayName = player.Name.Length > 11
                ? player.Name[..10] + "."
                : player.Name;
            var nameLabel = UITheme.CreateLabel(displayName,
                UITheme.FontSizeSmall, UITheme.TextDark, HorizontalAlignment.Center);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            vbox.AddChild(nameLabel);

            // Position
            string posText = outOfPosition ? $"{player.PrimaryPosition}->{slotPos}" : $"{slotPos}";
            Color posColor = outOfPosition ? UITheme.Red : UITheme.TextSecondary;
            var posLabel = UITheme.CreateLabel(posText,
                UITheme.FontSizeCaption, posColor, HorizontalAlignment.Center);
            posLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            vbox.AddChild(posLabel);
        }
        else
        {
            // Empty slot
            var slotLabel = UITheme.CreateLabel($"{slotPos}",
                UITheme.FontSizeSmall, new Color("FFFFFFAA"), HorizontalAlignment.Center);
            slotLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            slotLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            vbox.AddChild(slotLabel);
        }

        // Click to view player details
        if (hasPlayer)
        {
            int capturedId = playerId;
            panel.MouseFilter = MouseFilterEnum.Stop;
            panel.GuiInput += (InputEvent evt) =>
            {
                if (evt is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                {
                    EmitSignal(SignalName.PlayerClicked, capturedId);
                }
            };
        }

        return panel;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            RebuildCards();
        }
    }

    /// <summary>Converts FormationLayout tuples to Godot Vector2 list.</summary>
    private static IReadOnlyList<Vector2> GetPitchPositions(Formation formation)
    {
        IReadOnlyList<(float X, float Y)> tuples = FormationLayout.GetPositions(formation);
        return tuples.Select(t => new Vector2(t.X, t.Y)).ToArray();
    }
}
