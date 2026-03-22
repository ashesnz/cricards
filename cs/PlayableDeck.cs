using Godot;
using System.Collections.Generic;

public partial class PlayableDeck : Resource
{
    // Use a typed list for runtime storage to avoid Godot.Variant wrapping issues
    private List<CardWithID> _cards = new List<CardWithID>();

    // The exported Array can remain for editor/resource compatibility if desired,
    // but we prefer the typed list at runtime. (Keep export commented-out for now.)
    // [Export] public Godot.Collections.Array Cards { get; set; } = new Godot.Collections.Array();

    public int Size()
    {
        return _cards.Count;
    }

    public CardWithID? DealCard()
    {
        GD.Print($"[PLAYABLE_DECK DEBUG] DealCard: _cards.Count={_cards.Count}");
        if (_cards.Count == 0)
        {
            GD.Print("[PLAYABLE_DECK DEBUG] DealCard: returning null because _cards.Count==0");
            return null;
        }
        var val = _cards[_cards.Count - 1];
        _cards.RemoveAt(_cards.Count - 1);
        GD.Print($"[PLAYABLE_DECK DEBUG] DealCard: removed item, new _cards.Count={_cards.Count}");
        return val;
    }

    public void Shuffle()
    {
        // Fisher-Yates shuffle
        var rng = new RandomNumberGenerator(); rng.Randomize();
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = (int)rng.RandiRange(0, i);
            var tmp = _cards[i];
            _cards[i] = _cards[j];
            _cards[j] = tmp;
        }
    }

    public CardWithID? PeekTop()
    {
        if (_cards.Count == 0) return null;
        return _cards[_cards.Count - 1];
    }

    public void PutCardOnTop(CardWithID card)
    {
        if (card == null) return;
        _cards.Add(card);
    }

    public void PutCardOnBottom(CardWithID card)
    {
        if (card == null) return;
        _cards.Insert(0, card);
    }

    public CardWithID? GetRandomCard()
    {
        if (_cards.Count == 0) return null;
        var rng = new RandomNumberGenerator(); rng.Randomize();
        int idx = (int)rng.RandiRange(0, _cards.Count - 1);
        return _cards[idx];
    }

    // Helper to populate the runtime list from a Godot.Collections.Array of CardWithID
    public void SetFromArray(Godot.Collections.Array arr)
    {
        _cards.Clear();
        if (arr == null) return;
        foreach (object obj in arr)
        {
            var cw = obj as CardWithID;
            if (cw != null)
                _cards.Add(new CardWithID(cw.Id, cw.Card));
        }
        GD.Print($"[PLAYABLE_DECK DEBUG] SetFromArray: populated _cards.Count={_cards.Count}");
    }

    // Helper to expose current cards as a Godot Array when needed
    public Godot.Collections.Array ToArray()
    {
        var arr = new Godot.Collections.Array();
        foreach (var c in _cards)
            arr.Add(c);
        return arr;
    }
}
