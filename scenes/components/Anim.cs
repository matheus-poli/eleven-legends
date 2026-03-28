using Godot;

namespace ElevenLegends.UI;

/// <summary>
/// Duolingo-style animation helpers.
/// All methods are layout-safe: they only modify Modulate and Scale
/// (which don't affect container layout), never Position.
/// </summary>
public static class Anim
{
    /// <summary>
    /// Fades a node in from transparent to fully opaque.
    /// </summary>
    public static Tween FadeIn(Control node, float delay = 0f, float duration = -1f)
    {
        if (duration < 0) duration = UITheme.AnimNormal;
        node.Modulate = new Color(1, 1, 1, 0);

        Tween tween = node.CreateTween();
        if (delay > 0) tween.TweenInterval(delay);
        tween.TweenProperty(node, "modulate:a", 1f, duration)
            .SetEase(Tween.EaseType.Out);
        return tween;
    }

    /// <summary>
    /// Pops a node in: scale from 0.7 → 1.0 with overshoot + fade.
    /// Safe in containers — uses a one-shot timer to defer until after layout.
    /// </summary>
    public static void PopIn(Control node, float delay = 0f, float duration = -1f)
    {
        if (duration < 0) duration = UITheme.AnimSlow;
        node.Modulate = new Color(1, 1, 1, 0);
        node.Scale = new Vector2(0.7f, 0.7f);

        // Defer to next frame so Size is valid after layout
        float capturedDuration = duration;
        SceneTreeTimer timer = node.GetTree().CreateTimer(delay + 0.01f);
        timer.Timeout += () =>
        {
            if (!GodotObject.IsInstanceValid(node)) return;
            node.PivotOffset = node.Size / 2;

            Tween tween = node.CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(node, "scale", Vector2.One, capturedDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.TweenProperty(node, "modulate:a", 1f, capturedDuration * 0.6f)
                .SetEase(Tween.EaseType.Out);
        };
    }

    /// <summary>
    /// Bounces a node in from scale 0 → 1 with elastic overshoot.
    /// Best for hero elements (titles, trophies, badges).
    /// Call AFTER node has been added to tree and sized.
    /// </summary>
    public static Tween BounceIn(Control node, float delay = 0f, float duration = -1f)
    {
        if (duration < 0) duration = UITheme.AnimBounce;
        node.PivotOffset = node.Size / 2;
        node.Scale = Vector2.Zero;
        node.Modulate = new Color(1, 1, 1, 0);

        Tween tween = node.CreateTween();
        if (delay > 0) tween.TweenInterval(delay);
        tween.SetParallel(true);
        tween.TweenProperty(node, "scale", Vector2.One, duration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(node, "modulate:a", 1f, duration * 0.4f)
            .SetEase(Tween.EaseType.Out);
        return tween;
    }

    /// <summary>
    /// Quick scale pulse (1.0 → 1.15 → 1.0). Use for feedback on tap/click.
    /// </summary>
    public static Tween PulseOnce(Control node, float scale = 1.15f, float duration = -1f)
    {
        if (duration < 0) duration = UITheme.AnimFast * 2;
        node.PivotOffset = node.Size / 2;

        Tween tween = node.CreateTween();
        tween.TweenProperty(node, "scale", new Vector2(scale, scale), duration * 0.4f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(node, "scale", Vector2.One, duration * 0.6f)
            .SetEase(Tween.EaseType.Out);
        return tween;
    }

    /// <summary>
    /// Animates all children of a container with staggered fade-in.
    /// Each child appears <paramref name="stagger"/> seconds after the previous.
    /// Call after all children have been added.
    /// </summary>
    public static void StaggerChildren(Control container, float stagger = 0.06f,
        float duration = -1f, bool useScale = true)
    {
        if (duration < 0) duration = UITheme.AnimSlow;

        float delay = 0f;
        foreach (Node child in container.GetChildren())
        {
            if (child is not Control ctrl) continue;

            ctrl.Modulate = new Color(1, 1, 1, 0);
            if (useScale) ctrl.Scale = new Vector2(0.9f, 0.9f);

            float capturedDelay = delay;
            // Use a timer to defer animation start (ensures layout is done)
            SceneTreeTimer timer = ctrl.GetTree().CreateTimer(capturedDelay + 0.01f);
            timer.Timeout += () =>
            {
                if (!IsInstanceValid(ctrl)) return;
                if (useScale) ctrl.PivotOffset = ctrl.Size / 2;

                Tween tween = ctrl.CreateTween();
                tween.SetParallel(true);
                tween.TweenProperty(ctrl, "modulate:a", 1f, duration * 0.6f)
                    .SetEase(Tween.EaseType.Out);
                if (useScale)
                {
                    tween.TweenProperty(ctrl, "scale", Vector2.One, duration)
                        .SetEase(Tween.EaseType.Out)
                        .SetTrans(Tween.TransitionType.Back);
                }
            };

            delay += stagger;
        }
    }

    /// <summary>
    /// Continuous gentle floating motion (up ↔ down). Good for decorative icons.
    /// Returns the looping tween so caller can stop it.
    /// Call AFTER node is positioned.
    /// </summary>
    public static Tween FloatLoop(Control node, float amplitude = 8f, float cycleDuration = 2.5f)
    {
        Vector2 basePos = node.Position;

        Tween tween = node.CreateTween();
        tween.SetLoops(); // infinite
        tween.TweenProperty(node, "position:y", basePos.Y - amplitude, cycleDuration / 2f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(node, "position:y", basePos.Y + amplitude, cycleDuration / 2f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        return tween;
    }

    /// <summary>
    /// Continuous slow rotation. Good for decorative elements.
    /// </summary>
    public static Tween SpinLoop(Control node, float cycleDuration = 4f)
    {
        node.PivotOffset = node.Size / 2;

        Tween tween = node.CreateTween();
        tween.SetLoops();
        tween.TweenProperty(node, "rotation", Mathf.Tau, cycleDuration)
            .AsRelative()
            .SetTrans(Tween.TransitionType.Linear);
        return tween;
    }

    /// <summary>
    /// Checks if a Godot object instance is still valid (not freed).
    /// </summary>
    private static bool IsInstanceValid(GodotObject obj)
    {
        return GodotObject.IsInstanceValid(obj);
    }
}
