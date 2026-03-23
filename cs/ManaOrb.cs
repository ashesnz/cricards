using Godot;

public partial class ManaOrb : Sprite2D
{
    private Vector2 _original_position;
    private Tween? _fill_up_tween;
    private Tween? _spend_tween;
    private Tween? _empty_tween;

    private Label? label;
    private Sprite2D? mana_orb;

    // Expose the Label for external access (e.g., Main) safely
    public Label? Label => label;

    public override void _Ready()
    {
        _original_position = Position;
        label = GetNodeOrNull<Label>("Label");
        mana_orb = GetNodeOrNull<Sprite2D>(".");
    }

    public void FillUpAnimation()
    {
        if (_empty_tween != null)
            _empty_tween.Kill();

        // play ManaGlassSFX

        _fill_up_tween = CreateTween();
        _fill_up_tween.SetParallel(true);
        _fill_up_tween.SetTrans(Tween.TransitionType.Linear);
        _fill_up_tween.TweenProperty(this, "rotation", 0, 0.2f);
        _fill_up_tween.TweenProperty(this, "position", _original_position, 0.2f);
        _fill_up_tween.SetTrans(Tween.TransitionType.Bounce);
        _fill_up_tween.SetEase(Tween.EaseType.Out);
        _fill_up_tween.TweenProperty(this, "modulate", Colors.White, 0.5f);

        _fill_up_tween.SetParallel(false);
        _fill_up_tween.SetTrans(Tween.TransitionType.Back);
        _fill_up_tween.TweenProperty(this, "scale", new Vector2(1.1f, 1.1f), 0.2f);
        _fill_up_tween.TweenProperty(this, "scale", new Vector2(1.0f, 1.0f), 0.2f);
    }

    public void SpendAnimation()
    {
        _spend_tween = CreateTween();
        var duration = 0.15f;
        var amount = 0.15f;
        _spend_tween.TweenProperty(this, "rotation", -amount, duration);
        _spend_tween.TweenProperty(this, "rotation", amount, duration);
        _spend_tween.TweenProperty(this, "rotation", -amount, duration);
        _spend_tween.TweenProperty(this, "rotation", amount, duration);
        _spend_tween.TweenProperty(this, "rotation", 0, duration);
    }

    public void EmptyAnimation()
    {
        // play ManaGlassEMPTYSFX

        _empty_tween = CreateTween();
        _empty_tween.SetParallel();
        _empty_tween.SetTrans(Tween.TransitionType.Expo);
        _empty_tween.SetEase(Tween.EaseType.In);
        _empty_tween.TweenProperty(this, "position:y", Position.Y + 12.5f, 1.0f);
        _empty_tween.TweenProperty(this, "rotation", 0.5f, 1.0f);
        _empty_tween.SetTrans(Tween.TransitionType.Bounce);
        _empty_tween.SetEase(Tween.EaseType.Out);
        _empty_tween.TweenProperty(this, "modulate", new Color(0.6f, 0.6f, 0.6f), 1.0f);
    }
}

