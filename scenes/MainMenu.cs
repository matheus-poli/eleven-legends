using Godot;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Main menu — Duolingo-style splash with animated entrance.
/// Green gradient background, bouncing title, 3D raised buttons.
/// </summary>
public partial class MainMenu : Control
{
    public override void _Ready()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        // ─── Green gradient background ────────────────────────────
        var bg = UITheme.CreateGradientBackground(UITheme.Green, UITheme.GreenDark);
        AddChild(bg);

        // ─── Center container ─────────────────────────────────────
        var centerWrapper = new CenterContainer();
        centerWrapper.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(centerWrapper);

        var center = new VBoxContainer();
        center.AddThemeConstantOverride("separation", 12);
        centerWrapper.AddChild(center);

        // ─── Floating football icon ───────────────────────────────
        var iconLabel = UITheme.CreateIcon("football", new Vector2(72, 72));
        iconLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        center.AddChild(iconLabel);

        // ─── Title ────────────────────────────────────────────────
        var title = UITheme.CreateLabel("ELEVEN LEGENDS", UITheme.FontSizeDisplay,
            UITheme.White, HorizontalAlignment.Center);
        title.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        center.AddChild(title);

        // ─── Subtitle ─────────────────────────────────────────────
        var subtitle = UITheme.CreateLabel("Football Manager",
            UITheme.FontSizeBody, new Color(1, 1, 1, 0.7f), HorizontalAlignment.Center);
        subtitle.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        center.AddChild(subtitle);

        // ─── Spacer ───────────────────────────────────────────────
        center.AddChild(new Control { CustomMinimumSize = new Vector2(0, 32) });

        // ─── New Game button (yellow — stands out on green) ───────
        var newGameBtn = UITheme.CreateButton("New Game", UITheme.Yellow, UITheme.TextDark);
        newGameBtn.CustomMinimumSize = new Vector2(280, 60);
        newGameBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        newGameBtn.Pressed += OnNewGame;
        center.AddChild(newGameBtn);

        // ─── Online button (disabled — future feature) ────────────
        var onlineBtn = UITheme.CreateButton("Online", UITheme.Blue);
        onlineBtn.CustomMinimumSize = new Vector2(280, 60);
        onlineBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        onlineBtn.Disabled = true;
        onlineBtn.Modulate = new Color(1, 1, 1, 0.5f);
        center.AddChild(onlineBtn);

        // ─── Version text ─────────────────────────────────────────
        center.AddChild(new Control { CustomMinimumSize = new Vector2(0, 16) });
        var version = UITheme.CreateLabel("Pre-Alpha Demo",
            UITheme.FontSizeCaption, new Color(1, 1, 1, 0.4f), HorizontalAlignment.Center);
        version.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        center.AddChild(version);

        // ─── Entrance animations (deferred to after layout) ───────
        PlayEntranceAnimations(iconLabel, title, subtitle, newGameBtn);
    }

    private void PlayEntranceAnimations(Control icon, Control title, Control subtitle, Control button)
    {
        // Wait one frame for layout to settle
        GetTree().CreateTimer(0.05f).Timeout += () =>
        {
            if (!IsInsideTree()) return;

            // Icon: bounce in
            Anim.BounceIn(icon, delay: 0.1f, duration: 0.6f);

            // Title: bounce in slightly later
            Anim.BounceIn(title, delay: 0.25f, duration: 0.5f);

            // Subtitle: gentle fade
            Anim.FadeIn(subtitle, delay: 0.5f, duration: 0.4f);

            // Buttons: stagger pop-in — find all buttons in the tree
            float btnDelay = 0.65f;
            foreach (Node child in title.GetParent().GetChildren())
            {
                if (child is Button btn)
                {
                    btn.Modulate = new Color(1, 1, 1, 0);
                    btn.Scale = new Vector2(0.85f, 0.85f);

                    float d = btnDelay;
                    GetTree().CreateTimer(d).Timeout += () =>
                    {
                        if (!IsInstanceValid(btn)) return;
                        btn.PivotOffset = btn.Size / 2;
                        Tween tween = btn.CreateTween();
                        tween.SetParallel(true);
                        tween.TweenProperty(btn, "scale", Vector2.One, 0.4f)
                            .SetEase(Tween.EaseType.Out)
                            .SetTrans(Tween.TransitionType.Back);
                        tween.TweenProperty(btn, "modulate:a", 1f, 0.25f)
                            .SetEase(Tween.EaseType.Out);
                    };
                    btnDelay += 0.12f;
                }
            }

            // Icon: start floating after entrance
            GetTree().CreateTimer(0.8f).Timeout += () =>
            {
                if (IsInstanceValid(icon) && icon.IsInsideTree())
                    Anim.FloatLoop(icon, amplitude: 6f, cycleDuration: 3f);
            };
        };
    }

    private void OnNewGame()
    {
        SceneManager.Instance.ChangeScene("res://scenes/ClubSelection.tscn");
    }

}
