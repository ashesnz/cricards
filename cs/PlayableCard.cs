using Godot;
using Godot.Collections;
using System.Linq;

public partial class PlayableCard : Node2D
{
    [Signal] public delegate void PlayableCardMouseEnteredEventHandler(PlayableCard card);
    [Signal] public delegate void PlayableCardMouseExitedEventHandler(PlayableCard card);

    public Array actions = new Array();
    public int id = -1;
    public CardData? card_data;
    public bool exhausted = false;

    private Card? _card;
    public Card? Card => _card;

    // Expose a size hint for callers that still reference MinSize (e.g. CardContainer)
    public Vector2 MinSize => _card != null ? _card.MinSize : Vector2.Zero;

    public override void _Ready()
    {
        _card = GetNodeOrNull<Card>("Card");
        if (_card != null)
        {
            try { _card.CardMouseEntered += _on_card_mouse_entered; } catch { }
            try { _card.CardMouseExited  += _on_card_mouse_exited;  } catch { }
        }
    }

    public void LoadCardData(CardData cardData)
    {
        card_data = cardData;
        if (_card != null)
            _card.SetValues(card_data.Title, card_data.Description, card_data.Cost, card_data.CardType, card_data.Image);
        actions = card_data.Actions;
    }

    public void Highlight()   => _card?.Highlight();
    public void Unhighlight() => _card?.Unhighlight();
    public int  GetCost()     => _card != null ? _card.cost : 0;

    public void Activate(Dictionary game_state)
    {
        var ctx = new ActionContext(game_state ?? new Dictionary());
        foreach (var a in actions.OfType<Action>())
            a.Activate(ctx);
    }

    private void _on_card_mouse_entered(Card _c)
        => EmitSignal("PlayableCardMouseEntered", (GodotObject)this);

    private void _on_card_mouse_exited(Card _c)
        => EmitSignal("PlayableCardMouseExited", (GodotObject)this);
}
