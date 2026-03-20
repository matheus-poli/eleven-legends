using Godot;

namespace ElevenLegends.UI;

/// <summary>
/// Color palette and styling constants for the Eleven Legends UI.
/// Duolingo-inspired: vibrant colors, gradients, rounded corners.
/// </summary>
public static class UITheme
{
    // Primary palette
    public static readonly Color Green = new("4CAF50");
    public static readonly Color GreenDark = new("388E3C");
    public static readonly Color Blue = new("2196F3");
    public static readonly Color BlueDark = new("1565C0");
    public static readonly Color Yellow = new("FFC107");
    public static readonly Color YellowDark = new("F9A825");
    public static readonly Color Pink = new("E91E63");
    public static readonly Color PinkDark = new("C2185B");
    public static readonly Color Orange = new("FF9800");

    // Neutrals
    public static readonly Color White = new("FFFFFF");
    public static readonly Color Background = new("F5F7FA");
    public static readonly Color Card = new("FFFFFF");
    public static readonly Color TextPrimary = new("1A1A2E");
    public static readonly Color TextSecondary = new("6B7280");
    public static readonly Color TextLight = new("FFFFFF");
    public static readonly Color Border = new("E5E7EB");
    public static readonly Color Shadow = new("00000020");

    // Semantic
    public static readonly Color Success = Green;
    public static readonly Color Danger = Pink;
    public static readonly Color Warning = Yellow;
    public static readonly Color Info = Blue;

    // Sizes
    public const int FontSizeTitle = 32;
    public const int FontSizeHeading = 24;
    public const int FontSizeBody = 18;
    public const int FontSizeSmall = 14;
    public const int FontSizeCaption = 12;

    public const int CornerRadius = 16;
    public const int CardCornerRadius = 20;
    public const int ButtonCornerRadius = 12;
    public const int Padding = 16;
    public const int PaddingSmall = 8;
    public const int PaddingLarge = 24;

    /// <summary>
    /// Creates a styled button with rounded corners and color.
    /// </summary>
    public static Button CreateButton(string text, Color bgColor, Color? textColor = null)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(200, 56),
        };

        var style = new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusBottomLeft = ButtonCornerRadius,
            CornerRadiusBottomRight = ButtonCornerRadius,
            CornerRadiusTopLeft = ButtonCornerRadius,
            CornerRadiusTopRight = ButtonCornerRadius,
            ContentMarginLeft = Padding,
            ContentMarginRight = Padding,
            ContentMarginTop = PaddingSmall,
            ContentMarginBottom = PaddingSmall,
        };

        var hoverStyle = new StyleBoxFlat
        {
            BgColor = bgColor.Darkened(0.1f),
            CornerRadiusBottomLeft = ButtonCornerRadius,
            CornerRadiusBottomRight = ButtonCornerRadius,
            CornerRadiusTopLeft = ButtonCornerRadius,
            CornerRadiusTopRight = ButtonCornerRadius,
            ContentMarginLeft = Padding,
            ContentMarginRight = Padding,
            ContentMarginTop = PaddingSmall,
            ContentMarginBottom = PaddingSmall,
        };

        var pressedStyle = new StyleBoxFlat
        {
            BgColor = bgColor.Darkened(0.2f),
            CornerRadiusBottomLeft = ButtonCornerRadius,
            CornerRadiusBottomRight = ButtonCornerRadius,
            CornerRadiusTopLeft = ButtonCornerRadius,
            CornerRadiusTopRight = ButtonCornerRadius,
            ContentMarginLeft = Padding,
            ContentMarginRight = Padding,
            ContentMarginTop = PaddingSmall,
            ContentMarginBottom = PaddingSmall,
        };

        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeStyleboxOverride("focus", style);
        button.AddThemeFontSizeOverride("font_size", FontSizeBody);
        button.AddThemeColorOverride("font_color", textColor ?? TextLight);
        button.AddThemeColorOverride("font_hover_color", textColor ?? TextLight);
        button.AddThemeColorOverride("font_pressed_color", textColor ?? TextLight);

        return button;
    }

    /// <summary>
    /// Creates a card-style panel with rounded corners and shadow.
    /// </summary>
    public static PanelContainer CreateCard()
    {
        var panel = new PanelContainer();

        var style = new StyleBoxFlat
        {
            BgColor = Card,
            CornerRadiusBottomLeft = CardCornerRadius,
            CornerRadiusBottomRight = CardCornerRadius,
            CornerRadiusTopLeft = CardCornerRadius,
            CornerRadiusTopRight = CardCornerRadius,
            ContentMarginLeft = Padding,
            ContentMarginRight = Padding,
            ContentMarginTop = Padding,
            ContentMarginBottom = Padding,
            ShadowColor = Shadow,
            ShadowSize = 4,
            ShadowOffset = new Vector2(0, 2),
        };

        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    /// <summary>
    /// Creates a styled label.
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
    /// Creates a gradient background ColorRect.
    /// </summary>
    public static ColorRect CreateBackground(Color color)
    {
        return new ColorRect
        {
            Color = color,
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
        };
    }
}
