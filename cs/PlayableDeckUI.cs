using Godot;

public partial class PlayableDeckUI : TextureButton
{
    private Label? label;
    public PlayableDeck deck = new PlayableDeck();

    public override void _Ready()
    {
        label = GetNodeOrNull<Label>("Label");
    }

    public CardWithID? Draw()
    {
        SetLabelDeckSize();
        GD.Print($"[PLAYABLE_DECK_UI DEBUG] Draw called: deck.Size()={deck.Size()}");
        var result = deck.DealCard();
        GD.Print($"[PLAYABLE_DECK_UI DEBUG] Draw result={(result != null ? result.Id.ToString() : "null")}");
        return result;
    }

    public void AddCardOnTop(CardWithID? card_with_id)
    {
        if (card_with_id == null)
            return;
        deck.PutCardOnTop(card_with_id);
        SetLabelDeckSize();
    }

    public void AddCardOnBottom(CardWithID? card_with_id)
    {
        if (card_with_id == null)
            return;
        deck.PutCardOnBottom(card_with_id);
        SetLabelDeckSize();
    }

    public void SetLabelDeckSize()
    {
        if (label == null)
            return;

        label.Text = deck != null ? deck.Size().ToString() : "0";
    }

    public int GetNumberOfCards()
    {
        return deck != null ? deck.Size() : 0;
    }
}

