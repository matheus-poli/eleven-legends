using Godot;

namespace ElevenLegends.UI;

/// <summary>
/// Card panel with Duolingo-style 3D hover effect.
/// Tilts toward mouse position, scales up, and elevates on hover.
/// Use instead of UITheme.CreateCard() when you want interactive hover feel.
/// </summary>
public partial class HoverCard : PanelContainer
{
    private bool _isHovered;
    private Tween? _hoverTween;

    /// <summary>If true, skips the tilt effect (only scale + shadow).</summary>
    public bool DisableTilt { get; set; }

    public override void _Ready()
    {
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Process(double delta)
    {
        if (!_isHovered || DisableTilt) return;

        Vector2 localMouse = GetLocalMousePosition();
        Vector2 center = Size / 2;
        Vector2 offset = (localMouse - center) / Size; // -0.5 to 0.5

        // Tilt toward mouse — small rotation for subtle 3D feel
        float targetRotation = -offset.X * UITheme.CardHoverTilt;
        Rotation = Mathf.Lerp(Rotation, targetRotation, (float)delta * 12f);
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        PivotOffset = Size / 2;
        ZIndex = 1; // Render above siblings

        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.TweenProperty(this, "scale",
                new Vector2(UITheme.CardHoverScale, UITheme.CardHoverScale), UITheme.AnimNormal)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    private void OnMouseExited()
    {
        _isHovered = false;

        _hoverTween?.Kill();
        _hoverTween = CreateTween();
        _hoverTween.SetParallel(true);

        _hoverTween.TweenProperty(this, "scale", Vector2.One, UITheme.AnimNormal)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        _hoverTween.TweenProperty(this, "rotation", 0f, UITheme.AnimSlow)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);

        // Reset ZIndex after animation
        _hoverTween.Chain().TweenCallback(Callable.From(() => ZIndex = 0));
    }

    // ─── Factory ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a styled HoverCard with rounded corners, shadow, and optional accent stripe.
    /// </summary>
    public static HoverCard Create(Color? accentColor = null)
    {
        var card = new HoverCard();

        var style = new StyleBoxFlat
        {
            BgColor = UITheme.Card,
            CornerRadiusTopLeft = UITheme.CardCornerRadius,
            CornerRadiusTopRight = UITheme.CardCornerRadius,
            CornerRadiusBottomLeft = UITheme.CardCornerRadius,
            CornerRadiusBottomRight = UITheme.CardCornerRadius,
            ContentMarginLeft = UITheme.Padding,
            ContentMarginRight = UITheme.Padding,
            ContentMarginTop = UITheme.Padding,
            ContentMarginBottom = UITheme.Padding,
            ShadowColor = UITheme.Shadow,
            ShadowSize = 8,
            ShadowOffset = new Vector2(0, 4),
        };

        if (accentColor.HasValue)
        {
            style.BorderWidthTop = 4;
            style.BorderColor = accentColor.Value;
        }

        card.AddThemeStyleboxOverride("panel", style);
        return card;
    }
}
