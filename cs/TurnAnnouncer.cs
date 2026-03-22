using Godot;

public partial class TurnAnnouncer : Label
{
    public float TotalDuration = 2.0f;

    private Tween _tween;
    private Vector2 _original_position;

    public override void _Ready()
    {
        _original_position = Position;
        // Start hidden; Announce() will set Text and make visible when needed.
        Visible = false;
        _tween = null;
    }

    public Tween Announce(string announcement, float duration = -1)
    {
        if (duration < 0) duration = TotalDuration;
        Text = announcement;
        Visible = true;

        var offset = 1000;
        Position = _original_position;
        Scale = new Vector2(Scale.X, 0.0f);
        Position = new Vector2(Position.X + offset, Position.Y);

        if (_tween != null)
            _tween.Kill();
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Expo);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.SetParallel(true);
        _tween.TweenProperty(this, "scale:y", 1.0f, duration * 2 / 5);
        _tween.TweenProperty(this, "position:x", Position.X - offset, duration / 3);

        _tween.SetParallel(false);
        _tween.TweenInterval(duration / 3);

        _tween.SetEase(Tween.EaseType.In);
        _tween.SetParallel(true);
        _tween.TweenProperty(this, "scale:y", 0.0f, duration * 2 / 5);
        _tween.TweenProperty(this, "position:x", Position.X - 2 * offset, duration / 3);

        // Hide after the tween finishes
        _tween.Finished += () => { Visible = false; };

        return _tween;
    }
}

