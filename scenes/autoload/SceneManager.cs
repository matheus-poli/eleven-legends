using Godot;

namespace ElevenLegends.UI;

/// <summary>
/// Autoload singleton that manages scene transitions with fade effects.
/// Register as autoload in Project > Project Settings > Autoload.
/// </summary>
public partial class SceneManager : Node
{
    private static SceneManager? _instance;
    public static SceneManager Instance => _instance!;

    private ColorRect _fadeOverlay = null!;
    private AnimationPlayer _animator = null!;

    /// <summary>
    /// Shared game state accessible across all scenes.
    /// </summary>
    public GameState? CurrentGameState { get; set; }

    public override void _Ready()
    {
        _instance = this;

        // Create fade overlay for transitions
        var canvas = new CanvasLayer { Layer = 100 };
        AddChild(canvas);

        _fadeOverlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 1),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(1, 1, 1, 0), // Start transparent
        };
        canvas.AddChild(_fadeOverlay);
        _fadeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Create animator
        _animator = new AnimationPlayer();
        AddChild(_animator);

        // Fade-in animation
        var fadeIn = new Animation();
        fadeIn.Length = 0.3f;
        int trackIdx = fadeIn.AddTrack(Animation.TrackType.Value);
        fadeIn.TrackSetPath(trackIdx, _fadeOverlay.GetPath() + ":modulate:a");
        fadeIn.TrackInsertKey(trackIdx, 0, 1.0f);
        fadeIn.TrackInsertKey(trackIdx, 0.3f, 0.0f);

        // Fade-out animation
        var fadeOut = new Animation();
        fadeOut.Length = 0.3f;
        int trackIdx2 = fadeOut.AddTrack(Animation.TrackType.Value);
        fadeOut.TrackSetPath(trackIdx2, _fadeOverlay.GetPath() + ":modulate:a");
        fadeOut.TrackInsertKey(trackIdx2, 0, 0.0f);
        fadeOut.TrackInsertKey(trackIdx2, 0.3f, 1.0f);

        var lib = new AnimationLibrary();
        lib.AddAnimation("fade_in", fadeIn);
        lib.AddAnimation("fade_out", fadeOut);
        _animator.AddAnimationLibrary("transitions", lib);
    }

    /// <summary>
    /// Changes scene with a fade transition.
    /// </summary>
    public async void ChangeScene(string scenePath)
    {
        _fadeOverlay.MouseFilter = Control.MouseFilterEnum.Stop;

        // Fade out
        _animator.Play("transitions/fade_out");
        await ToSignal(_animator, AnimationPlayer.SignalName.AnimationFinished);

        // Change scene
        GetTree().ChangeSceneToFile(scenePath);

        // Fade in
        _animator.Play("transitions/fade_in");
        await ToSignal(_animator, AnimationPlayer.SignalName.AnimationFinished);

        _fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// Changes scene without fade (instant).
    /// </summary>
    public void ChangeSceneInstant(string scenePath)
    {
        GetTree().ChangeSceneToFile(scenePath);
    }
}
