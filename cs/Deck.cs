using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class Deck : Resource
{
    // Use a typed generic dictionary so values remain CardWithID instances instead of Godot.Variant wrappers
    private System.Collections.Generic.Dictionary<int, CardWithID> _card_collection = new System.Collections.Generic.Dictionary<int, CardWithID>();
    private int _id_counter = 0;

    public void AddCard(CardData card)
    {
        // Debug: print incoming card info and collection size
        try
        {
            GD.Print($"[DECK DEBUG] AddCard called: card_set={(card != null)} title={(card != null ? card.Title : "<null>")} before_count={_card_collection.Count}");
        }
        catch (Exception ex)
        {
            GD.PrintErr("[DECK DEBUG] AddCard: error printing card info: " + ex.Message);
        }
        var card_id = _generate_card_id(card);
        var c = new CardWithID(card_id, card);
        _card_collection[card_id] = c;
        GD.Print($"[DECK DEBUG] AddCard finished: added id={card_id} after_count={_card_collection.Count}");
    }

    public void RemoveCardByData(CardData card_data)
    {
        int? removeKey = null;
        foreach (var kv in _card_collection)
        {
            var key = kv.Key;
            var card_with_id = kv.Value;
            if (card_with_id != null && card_with_id.Card != null && card_with_id.Card.Title == card_data.Title)
            {
                removeKey = key;
                break;
            }
        }
        if (removeKey.HasValue)
            _card_collection.Remove(removeKey.Value);
    }

    public void RemoveCard(int card_id)
    {
        if (_card_collection.ContainsKey(card_id))
            _card_collection.Remove(card_id);
    }

    public void UpdateCard(int card_id, CardData card)
    {
        _card_collection[card_id] = new CardWithID(card_id, card);
    }

    public Godot.Collections.Array GetCards()
    {
        var cards = new Godot.Collections.Array();
        GD.Print($"[DECK DEBUG] GetCards: _card_collection.Count={_card_collection.Count}");
        if (_card_collection.Count > 0)
        {
            foreach (var c in _card_collection.Values)
            {
                if (c != null)
                {
                    var cw = new CardWithID(c.Id, c.Card);
                    cards.Add(cw);
                    GD.Print($"[DECK DEBUG] GetCards: added CardWithID id={c.Id} title={(c.Card != null ? c.Card.Title : "<no card>")}");
                }
                else
                {
                    GD.PrintErr("[DECK DEBUG] GetCards: encountered null CardWithID value");
                }
            }
        }
        GD.Print($"[DECK DEBUG] GetCards: returning cards.Count={cards.Count}");
        return cards;
    }

    public CardWithID? GetCard(int id)
    {
        if (_card_collection.ContainsKey(id))
        {
            var c = _card_collection[id];
            return new CardWithID(c.Id, c.Card);
        }
        Godot.GD.PrintErr("Deck.cs => Error: couldn't find id: " + id);
        return null;
    }

    public PlayableDeck GetPlayableDeck()
    {
        var playable_deck = new PlayableDeck();
        // Populate the PlayableDeck directly from the typed _card_collection to avoid
        // Godot.Collections.Array -> Variant conversion issues at runtime.
        foreach (var c in _card_collection.Values)
        {
            if (c != null)
                playable_deck.PutCardOnTop(new CardWithID(c.Id, c.Card));
        }
        GD.Print($"[DECK DEBUG] GetPlayableDeck: populated playable_deck.Size()={playable_deck.Size()}");
        return playable_deck;
    }

    public void Shuffle()
    {
        var card_list = new Godot.Collections.Array();
        foreach (object valObj in _card_collection.Values)
        {
            var c = valObj as CardWithID;
            if (c != null)
                card_list.Add(c);
        }
        card_list.Shuffle();

        _card_collection.Clear();
        foreach (object card_with_id in card_list)
        {
            var c = card_with_id as CardWithID;
            if (c != null)
                _card_collection[c.Id] = c;
        }
    }

    private int _generate_card_id(CardData card)
    {
        _id_counter += 1;
        return _id_counter;
    }
}

