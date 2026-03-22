using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class PlayableCard : Control
{
    [Signal] public delegate void MouseEnteredEventHandler(PlayableCard card);
    [Signal] public delegate void MouseExitedEventHandler(PlayableCard card);

    public Array actions = new Array();
    public int id = -1;
    public CardData? card_data;
    public bool exhausted = false;

    private Card card;

    // Provide read-only access to the underlying Card node for other systems
    public Card? Card => card;

    public override void _Ready()
    {
        card = GetNodeOrNull<Card>("Card");
        if (card != null)
        {
            card.MouseEntered += _on_card_mouse_entered;
            card.MouseExited += _on_card_mouse_exited;
        }
    }

    public void LoadCardData(CardData cardData)
    {
        card_data = cardData;
        if (card != null)
        {
            card.SetValues(card_data.Title, card_data.Description, card_data.Cost, card_data.CardType, card_data.Image);
        }

        // Convert action script references (Script resources) into Action instances at runtime is complex.
        // For now, just store the script resources; you can extend to instantiate specific Action classes.
        actions = card_data.Actions;
    }

    public void Highlight() => card?.Highlight();
    public void Unhighlight() => card?.Unhighlight();
    public int GetCost() => card != null ? card.cost : 0;

    public void Activate(Dictionary game_state)
    {
        var ctx = new ActionContext(game_state ?? new Dictionary());
        foreach (var a in actions.OfType<Action>())
            a.Activate(ctx);
    }

    private void _on_card_mouse_entered(Card c)
    {
        EmitSignal(SignalName.MouseEntered, this);
    }

    private void _on_card_mouse_exited(Card c)
    {
        EmitSignal(SignalName.MouseExited, this);
    }
}

