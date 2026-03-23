using Godot;

public partial class RewardCardContainer : Control
{
    [Signal] public delegate void ChosenEventHandler(PlayableCard playable_card);

    public bool mouse_over = false;

    private PlayableCard? playable_card;
    public PlayableCard? PlayableCard => playable_card;

    public override void _Ready()
    {
        playable_card = GetNodeOrNull<PlayableCard>("PlayableCard");
        if (playable_card != null)
        {
            playable_card.MouseEntered += _OnMouseEntered;
            playable_card.MouseExited += _OnMouseExited;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("mouse_click") && mouse_over && playable_card != null)
            EmitSignal(SignalName.Chosen, playable_card);
    }

    private void _OnMouseEntered(PlayableCard pc)
    {
        mouse_over = true;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Circ);
        tween.SetEase(Tween.EaseType.Out);
        if (playable_card != null)
            tween.TweenProperty(playable_card, "scale", Vector2.One * 1.375f, 0.75f);
    }

    private void _OnMouseExited(PlayableCard pc)
    {
        mouse_over = false;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Circ);
        tween.SetEase(Tween.EaseType.Out);
        if (playable_card != null)
            tween.TweenProperty(playable_card, "scale", Vector2.One, 0.75f);
    }
}

