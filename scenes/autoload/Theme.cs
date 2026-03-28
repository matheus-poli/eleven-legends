using Godot;

namespace ElevenLegends.UI;

/// <summary>
/// Duolingo-inspired UI theme for Eleven Legends.
/// 3D raised buttons, vibrant colors, smooth animations.
/// </summary>
public static class UITheme
{
    // ─── Duolingo-bright palette ──────────────────────────────────────

    // Greens — primary action
    public static readonly Color Green = new("58CC02");
    public static readonly Color GreenDark = new("58A700");
    public static readonly Color GreenLight = new("89E219");

    // Blues — info, secondary
    public static readonly Color Blue = new("1CB0F6");
    public static readonly Color BlueDark = new("1899D6");

    // Reds — danger, error (aliased as Pink for backward compat)
    public static readonly Color Red = new("FF4B4B");
    public static readonly Color RedDark = new("EA2B2B");
    public static readonly Color Pink = new("FF4B4B");
    public static readonly Color PinkDark = new("EA2B2B");

    // Yellows — warning, gold, coins
    public static readonly Color Yellow = new("FFC800");
    public static readonly Color YellowDark = new("E5B400");

    // Purples — special, rare
    public static readonly Color Purple = new("CE82FF");
    public static readonly Color PurpleDark = new("B066E0");

    // Oranges — transfer, energy
    public static readonly Color Orange = new("FF9600");
    public static readonly Color OrangeDark = new("E58600");

    // ─── Neutrals ─────────────────────────────────────────────────────

    public static readonly Color White = new("FFFFFF");
    public static readonly Color Background = new("F7F7F7");
    public static readonly Color Card = new("FFFFFF");
    public static readonly Color TextPrimary = new("4B4B4B");
    public static readonly Color TextSecondary = new("AFAFAF");
    public static readonly Color TextLight = new("FFFFFF");
    public static readonly Color TextDark = new("3C3C3C");
    public static readonly Color Border = new("E5E5E5");
    public static readonly Color Shadow = new("00000020");
    public static readonly Color Overlay = new("00000066");

    // ─── Semantic aliases ─────────────────────────────────────────────

    public static readonly Color Success = Green;
    public static readonly Color Danger = Red;
    public static readonly Color Warning = Yellow;
    public static readonly Color Info = Blue;

    // ─── Player card rating tiers ─────────────────────────────────────

    public static readonly Color RatingGold = new("FFD700");
    public static readonly Color RatingGoldDark = new("DAA520");
    public static readonly Color RatingSilver = new("C0C0C0");
    public static readonly Color RatingSilverDark = new("A0A0A0");
    public static readonly Color RatingBronze = new("CD7F32");
    public static readonly Color RatingBronzeDark = new("A0652A");

    // ─── Typography ───────────────────────────────────────────────────

    public const int FontSizeDisplay = 48;
    public const int FontSizeTitle = 36;
    public const int FontSizeHeading = 26;
    public const int FontSizeBody = 18;
    public const int FontSizeSmall = 14;
    public const int FontSizeCaption = 12;

    // ─── Spacing & corners ────────────────────────────────────────────

    public const int CornerRadius = 16;
    public const int CardCornerRadius = 20;
    public const int ButtonCornerRadius = 16;
    public const int BadgeCornerRadius = 10;
    public const int Padding = 16;
    public const int PaddingSmall = 8;
    public const int PaddingLarge = 24;
    public const int PaddingXL = 32;

    // ─── Animation constants ──────────────────────────────────────────

    public const float AnimFast = 0.15f;
    public const float AnimNormal = 0.25f;
    public const float AnimSlow = 0.4f;
    public const float AnimBounce = 0.5f;

    /// <summary>Height in px of the 3D shadow under raised buttons.</summary>
    public const int ButtonShadowHeight = 6;

    /// <summary>Scale multiplier on card hover (1.04 = 4% larger).</summary>
    public const float CardHoverScale = 1.04f;

    /// <summary>Max rotation in radians for card tilt (~2°).</summary>
    public const float CardHoverTilt = 0.035f;

    // ═══════════════════════════════════════════════════════════════════
    //  Component factories
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Duolingo-style 3D raised button.
    /// Bottom border acts as a shadow; on press, the button "pushes down"
    /// (shadow shrinks, text shifts down). Total height stays constant.
    /// </summary>
    public static Button CreateButton(string text, Color bgColor, Color? textColor = null)
    {
        Color shadow = bgColor.Darkened(0.25f);
        int sh = ButtonShadowHeight;

        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(200, 56),
        };

        // Normal: raised
        var normalStyle = MakeButtonBox(bgColor, shadow, sh, false);
        // Hover: brighter
        var hoverStyle = MakeButtonBox(bgColor.Lightened(0.07f), shadow, sh, false);
        // Pressed: pushed down
        var pressedStyle = MakeButtonBox(bgColor.Darkened(0.05f), shadow, 2, true);
        // Focus: same as normal
        var focusStyle = MakeButtonBox(bgColor, shadow, sh, false);

        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeStyleboxOverride("focus", focusStyle);
        button.AddThemeFontSizeOverride("font_size", FontSizeBody);

        Color fc = textColor ?? TextLight;
        button.AddThemeColorOverride("font_color", fc);
        button.AddThemeColorOverride("font_hover_color", fc);
        button.AddThemeColorOverride("font_pressed_color", fc);
        button.AddThemeColorOverride("font_focus_color", fc);

        return button;
    }

    /// <summary>
    /// Flat button variant — no 3D shadow. Useful for tab bars and secondary actions.
    /// </summary>
    public static Button CreateFlatButton(string text, Color bgColor, Color? textColor = null)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(120, 44),
        };

        var style = MakeRoundedBox(bgColor, ButtonCornerRadius, PaddingSmall + 2);
        var hover = MakeRoundedBox(bgColor.Lightened(0.07f), ButtonCornerRadius, PaddingSmall + 2);
        var pressed = MakeRoundedBox(bgColor.Darkened(0.1f), ButtonCornerRadius, PaddingSmall + 2);

        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", style);
        button.AddThemeFontSizeOverride("font_size", FontSizeSmall);

        Color fc = textColor ?? TextLight;
        button.AddThemeColorOverride("font_color", fc);
        button.AddThemeColorOverride("font_hover_color", fc);
        button.AddThemeColorOverride("font_pressed_color", fc);

        return button;
    }

    /// <summary>
    /// Card panel with rounded corners and drop shadow.
    /// Optional accent stripe on top (colored top border).
    /// </summary>
    public static PanelContainer CreateCard(Color? accentColor = null)
    {
        var panel = new PanelContainer();

        var style = new StyleBoxFlat
        {
            BgColor = Card,
            CornerRadiusTopLeft = CardCornerRadius,
            CornerRadiusTopRight = CardCornerRadius,
            CornerRadiusBottomLeft = CardCornerRadius,
            CornerRadiusBottomRight = CardCornerRadius,
            ContentMarginLeft = Padding,
            ContentMarginRight = Padding,
            ContentMarginTop = Padding,
            ContentMarginBottom = Padding,
            ShadowColor = Shadow,
            ShadowSize = 8,
            ShadowOffset = new Vector2(0, 4),
        };

        if (accentColor.HasValue)
        {
            style.BorderWidthTop = 4;
            style.BorderColor = accentColor.Value;
        }

        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    /// <summary>
    /// Styled label with font size and color.
    /// </summary>
    public static Label CreateLabel(string text, int fontSize = FontSizeBody,
        Color? color = null, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = align,
        };

        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color ?? TextPrimary);

        return label;
    }

    /// <summary>
    /// Solid color background filling the entire screen.
    /// </summary>
    public static ColorRect CreateBackground(Color color)
    {
        return new ColorRect
        {
            Color = color,
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
        };
    }

    /// <summary>
    /// Vertical gradient background (top → bottom).
    /// </summary>
    public static TextureRect CreateGradientBackground(Color top, Color bottom)
    {
        var gradient = new Gradient();
        gradient.Colors = [top, bottom];
        gradient.Offsets = [0f, 1f];

        var tex = new GradientTexture2D
        {
            Gradient = gradient,
            Fill = GradientTexture2D.FillEnum.Linear,
            FillFrom = new Vector2(0.5f, 0f),
            FillTo = new Vector2(0.5f, 1f),
            Width = 4,
            Height = 256,
        };

        return new TextureRect
        {
            Texture = tex,
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
    }

    /// <summary>
    /// Rounded colored badge (e.g. overall rating number).
    /// </summary>
    public static PanelContainer CreateBadge(string text, Color bgColor,
        Color? textColor = null, int fontSize = FontSizeHeading, Vector2? minSize = null)
    {
        var panel = new PanelContainer();

        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = BadgeCornerRadius,
            CornerRadiusTopRight = BadgeCornerRadius,
            CornerRadiusBottomLeft = BadgeCornerRadius,
            CornerRadiusBottomRight = BadgeCornerRadius,
            ContentMarginLeft = PaddingSmall + 2,
            ContentMarginRight = PaddingSmall + 2,
            ContentMarginTop = PaddingSmall - 2,
            ContentMarginBottom = PaddingSmall - 2,
        };

        panel.AddThemeStyleboxOverride("panel", style);
        panel.CustomMinimumSize = minSize ?? new Vector2(52, 52);

        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", textColor ?? TextLight);
        panel.AddChild(label);

        return panel;
    }

    /// <summary>
    /// Horizontal progress bar with rounded corners and custom colors.
    /// </summary>
    public static ProgressBar CreateProgressBar(float value, float max, Color fillColor,
        Color? bgColor = null, Vector2? minSize = null)
    {
        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = max,
            Value = value,
            ShowPercentage = false,
            CustomMinimumSize = minSize ?? new Vector2(0, 16),
        };

        var bgStyle = MakeRoundedBox(bgColor ?? Border, 8, 0);
        var fillStyle = MakeRoundedBox(fillColor, 8, 0);

        bar.AddThemeStyleboxOverride("background", bgStyle);
        bar.AddThemeStyleboxOverride("fill", fillStyle);

        return bar;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the card tier color based on a player's overall rating.
    /// Gold (80+), Silver (60–79), Bronze (&lt;60).
    /// </summary>
    public static Color RatingColor(float overall)
    {
        return overall switch
        {
            >= 80 => RatingGold,
            >= 60 => RatingSilver,
            _ => RatingBronze,
        };
    }

    /// <summary>
    /// Returns a bright color for stat value display.
    /// </summary>
    public static Color StatColor(int value)
    {
        return value switch
        {
            >= 80 => Green,
            >= 60 => Blue,
            >= 40 => Yellow,
            _ => Red,
        };
    }

    /// <summary>
    /// Creates a simple horizontal separator line.
    /// </summary>
    public static HSeparator CreateSeparator()
    {
        var sep = new HSeparator();
        var style = new StyleBoxFlat
        {
            BgColor = Border,
            ContentMarginTop = 1,
            ContentMarginBottom = 1,
        };
        sep.AddThemeStyleboxOverride("separator", style);
        return sep;
    }

    // ─── Private style builders ───────────────────────────────────────

    private static StyleBoxFlat MakeButtonBox(Color bg, Color borderColor, int bottomBorder, bool pushDown)
    {
        int sh = ButtonShadowHeight;
        return new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = ButtonCornerRadius,
            CornerRadiusTopRight = ButtonCornerRadius,
            CornerRadiusBottomLeft = ButtonCornerRadius,
            CornerRadiusBottomRight = ButtonCornerRadius,
            ContentMarginLeft = Padding + 4,
            ContentMarginRight = Padding + 4,
            ContentMarginTop = pushDown ? PaddingSmall + sh - 2 : PaddingSmall,
            ContentMarginBottom = pushDown ? PaddingSmall + 2 : PaddingSmall + sh,
            BorderWidthBottom = bottomBorder,
            BorderColor = borderColor,
        };
    }

    private static StyleBoxFlat MakeRoundedBox(Color bg, int radius, int padding)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = padding,
            ContentMarginRight = padding,
            ContentMarginTop = padding,
            ContentMarginBottom = padding,
        };
    }
}
