using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Hand : Node2D
{
    [Signal] public delegate void CardActivatedEventHandler(PlayableCard card);

    /// <summary>Radius of the imaginary circle the cards are placed on.</summary>
    [Export] public float hand_radius = 1000.0f;

    /// <summary>Starting angle of the arc (degrees). -90 = straight up.</summary>
    [Export] public float card_angle = -90.0f;

    /// <summary>Total spread angle budget (degrees) shared across all cards.</summary>
    [Export] public float angle_limit = 30.0f;

    /// <summary>Maximum degrees between adjacent cards.</summary>
    [Export] public float max_card_spread_angle = 10.0f;

    /// <summary>How many pixels a hovered card lifts upward.</summary>
    [Export] public float hover_lift = 80.0f;

    // Public list so Main can iterate cards (e.g. to empty them)
    public List<PlayableCard> cards { get; } = new List<PlayableCard>();

    private HashSet<PlayableCard> _hoveredCards = new HashSet<PlayableCard>();
    private int _currentSelectedCardIndex = -1;

    // Baseline positions (local to Hand) computed by RepositionCards,
    // used by hover handlers to lift cards relative to their rest position.
    private Dictionary<PlayableCard, Vector2> _baselinePositions = new Dictionary<PlayableCard, Vector2>();
    private Dictionary<PlayableCard, float>   _baselineRotations = new Dictionary<PlayableCard, float>();

    public override void _Ready()
    {
        RepositionCards();
    }

    public override void _Process(double delta)
    {
        // Reset every card each frame, then highlight the topmost hovered one.
        _currentSelectedCardIndex = -1;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null) continue;
            card.Unhighlight();
            card.ZIndex = i;
        }

        if (_hoveredCards.Count > 0)
        {
            int highest = -1;
            foreach (var hovered in _hoveredCards)
            {
                int idx = cards.IndexOf(hovered);
                if (idx > highest) highest = idx;
            }
            if (highest >= 0 && highest < cards.Count)
            {
                cards[highest].Highlight();
                cards[highest].ZIndex = cards.Count;
                _currentSelectedCardIndex = highest;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("mouse_click") && _currentSelectedCardIndex >= 0)
        {
            EmitSignal(SignalName.CardActivated, cards[_currentSelectedCardIndex]);
            _currentSelectedCardIndex = -1;
        }
    }

    // --- Public API (matches the original interface used by Main.cs) ---

    public void AddCard(PlayableCard card)
    {
        cards.Add(card);
        AddChild(card);
        card.Visible = true;

        card.PlayableCardMouseEntered += _ => _HandleCardTouched(card);
        card.PlayableCardMouseExited  += _ => _HandleCardUntouched(card);

        RepositionCards();
    }

    public PlayableCard? RemoveCard(int index)
    {
        if (index < 0 || index >= cards.Count) return null;

        var card = cards[index];
        cards.RemoveAt(index);
        _hoveredCards.Remove(card);
        _baselinePositions.Remove(card);
        _baselineRotations.Remove(card);

        if (card.GetParent() == this)
            RemoveChild(card);

        CreateTween().TweenCallback(Callable.From(RepositionCards)).SetDelay(0.2f);
        return card;
    }

    public PlayableCard? RemoveByEntity(PlayableCard card)
        => RemoveCard(cards.IndexOf(card));

    public IReadOnlyList<PlayableCard> Empty()
    {
        _currentSelectedCardIndex = -1;
        var temp = cards.ToList();
        foreach (var card in temp)
        {
            if (card.GetParent() == this)
                RemoveChild(card);
        }
        cards.Clear();
        _hoveredCards.Clear();
        _baselinePositions.Clear();
        _baselineRotations.Clear();
        return temp;
    }

    // --- Private helpers ---

    private void _HandleCardTouched(PlayableCard card)
    {
        _hoveredCards.Add(card);

        // Lift the card above its baseline
        if (_baselinePositions.TryGetValue(card, out var baseline) &&
            _baselineRotations.TryGetValue(card, out var rot))
        {
            _AnimateCard(card, baseline - new Vector2(0, hover_lift), rot);
        }
    }

    private void _HandleCardUntouched(PlayableCard card)
    {
        _hoveredCards.Remove(card);

        // Return to baseline
        if (_baselinePositions.TryGetValue(card, out var baseline) &&
            _baselineRotations.TryGetValue(card, out var rot))
        {
            _AnimateCard(card, baseline, rot);
        }
    }

    /// <summary>
    /// Distributes all cards evenly along the bottom arc of the circle,
    /// exactly matching the GDScript reference's _reposition_cards logic.
    /// </summary>
    private void RepositionCards()
    {
        if (cards.Count == 0) return;

        float cardSpread = Mathf.Min(angle_limit / cards.Count, max_card_spread_angle);
        float currentAngle = -(cardSpread * (cards.Count - 1)) / 2.0f - 90.0f;

        _baselinePositions.Clear();
        _baselineRotations.Clear();

        foreach (var card in cards)
        {
            if (card == null) continue;
            Vector2 pos = _GetCardPosition(currentAngle);
            float rot = Mathf.DegToRad(currentAngle + 90.0f);

            _baselinePositions[card] = pos;
            _baselineRotations[card] = rot;

            _AnimateCard(card, pos, rot);
            currentAngle += cardSpread;
        }
    }

    private void _AnimateCard(PlayableCard card, Vector2 targetPos, float targetRot)
    {
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(card, "position", targetPos, 0.2f);
        tween.TweenProperty(card, "rotation", targetRot, 0.6f);
    }

    private Vector2 _GetCardPosition(float angleInDegrees)
    {
        float x = hand_radius * Mathf.Cos(Mathf.DegToRad(angleInDegrees));
        float y = hand_radius * Mathf.Sin(Mathf.DegToRad(angleInDegrees));
        return new Vector2(x, y);
    }
}
