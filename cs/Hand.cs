using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Hand : Node2D
{
    [Signal] public delegate void CardActivatedEventHandler(PlayableCard card);

    // How far cards spread horizontally from the centre of the hand.
    // Increased a bit to spread cards out more by default.
    [Export] public float arc_width = 520.0f;

    // Minimum desired horizontal gap (in pixels) between adjacent card centres.
    // The hand will increase its effective arc width to ensure at least this gap
    // between cards when necessary.
    [Export] public float min_card_gap = 360.0f;
    
    // Multiplier applied to the effective arc width to allow an easy global
    // increase in spacing without changing the raw arc_width value.
    [Export] public float spread_multiplier = 4.0f;

    // Extra padding (pixels) added between cards beyond their visual halves.
    [Export] public float min_card_padding = 8.0f;

    // When true, ensure step between card centres is at least the full visual
    // card width + padding (stronger guarantee against overlap).
    [Export] public bool force_no_overlap = true;

    // Debug / testing: force default spacing values at runtime when true.
    // Enabled by default to apply stronger spacing immediately for testing.
    [Export] public bool force_defaults = true;
    [Export] public float default_force_arc_width = 520.0f;
    [Export] public float default_force_min_card_gap = 360.0f;
    [Export] public float default_force_spread_multiplier = 4.0f;

    // When true, prints computed effective arc width & spacing info to the console.
    [Export] public bool debug_log_spacing = false;

    // Vertical depth of the arc — larger = cards dip down more at the sides.
    [Export] public float arc_height = 80.0f;

    // How much a hovered card lifts upward (in pixels).
    [Export] public float hover_lift = 80.0f;

    // Maximum rotation applied to the outermost cards (degrees).
    [Export] public float max_rotation_deg = 12.0f;

    private HashSet<PlayableCard> _hoveredCards = new();
    public List<PlayableCard> cards = new();

    private CollisionShape2D? _collisionShape;

    // The selected card is the hovered card with the highest index — pure derived state.
    private int SelectedCardIndex =>
        _hoveredCards.Count == 0
            ? -1
            : _hoveredCards.Max(c => cards.IndexOf(c));

    public override void _Ready()
    {
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        // If requested, force the defaults at runtime so inspector overrides
        // don't prevent us from testing wider spacing quickly.
        if (force_defaults)
        {
            arc_width = default_force_arc_width;
            min_card_gap = default_force_min_card_gap;
            spread_multiplier = default_force_spread_multiplier;
        }

        // Ensure layout reflects any changes.
        RepositionCards();
    }

    public override void _Process(double delta)
    {
        int selectedIndex = SelectedCardIndex;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card is null) continue;

            bool isSelected = i == selectedIndex;
            card.ZIndex = isSelected ? cards.Count : i;

            if (isSelected)
                card.Highlight();
            else
                card.Unhighlight();
        }
    }

    public override void _Input(InputEvent @event)
    {
        int selectedIndex = SelectedCardIndex;
        if (@event.IsActionPressed("mouse_click") && selectedIndex >= 0)
            EmitSignal(SignalName.CardActivated, cards[selectedIndex]);
    }

    // --- Public API ---

    public void AddCard(PlayableCard card)
    {
        cards.Add(card);
        AddChild(card);
        card.Visible = true;

        card.MouseEntered += _ => OnCardHovered(card);
        card.MouseExited  += _ => OnCardUnhovered(card);

        RepositionCards();
    }

    public PlayableCard? RemoveCard(int index)
    {
        if (index < 0 || index >= cards.Count) return null;

        var card = cards[index];
        cards.RemoveAt(index);
        _hoveredCards.Remove(card);
        RemoveChild(card);

        CreateTween()
            .TweenCallback(Callable.From(RepositionCards))
            .SetDelay(0.2f);

        return card;
    }

    public PlayableCard? RemoveByEntity(PlayableCard card)
        => RemoveCard(cards.IndexOf(card));

    public IReadOnlyList<PlayableCard> Empty()
    {
        var removed = cards.Where(c => c is not null).ToList();
        foreach (var card in removed) RemoveChild(card);
        cards.Clear();
        _hoveredCards.Clear();
        return removed;
    }

    // --- Layout ---

    /// <summary>
    /// Positions cards in a Slay the Spire style fan:
    ///   - Cards spread along a wide, shallow elliptical arc.
    ///   - The card at t=0 (centre) sits highest; outer cards dip down and rotate outward.
    ///   - Hovered cards lift upward.
    /// </summary>
    private void RepositionCards()
    {
        int count = cards.Count;
        if (count == 0) return;

        // Compute an effective arc width so that adjacent cards are at least
        // `min_card_gap` pixels apart. For n cards the delta x between adjacent
        // cards is (2 * arc_width) / (n - 1), so solve for arc_width.
        float effectiveArcWidth = ComputeEffectiveArcWidth(count);

        for (int i = 0; i < count; i++)
        {
            // Normalised position along the hand: -1 (left) to 0 (centre) to +1 (right).
            float t = count == 1 ? 0f : Mathf.Lerp(-1f, 1f, (float)i / (count - 1));

            AnimateCard(cards[i], ArcPosition(t, effectiveArcWidth), ArcRotation(t));
        }
    }

    /// <summary>Maps t in [-1, 1] to a point on the elliptical arc.</summary>
    private Vector2 ArcPosition(float t, float effectiveArcWidth)
    {
        // x spreads linearly; y uses a parabola so centre cards sit higher than edge cards.
        float x = t * effectiveArcWidth;
        float y = (t * t) * arc_height;
        return new Vector2(x, y);
    }

    /// <summary>Cards rotate to fan out from the bottom centre, matching the arc tangent.</summary>
    private float ArcRotation(float t)
        => Mathf.DegToRad(t * max_rotation_deg);

    /// <summary>
    /// Compute an effective arc width for `count` cards. Attempts to read the
    /// visual card width from the first available card texture; falls back to
    /// `min_card_gap` if not available. Ensures adjacent card centres are at
    /// least the desired gap so overlap is minimal.
    /// </summary>
    private float ComputeEffectiveArcWidth(int count)
    {
        float effective = arc_width;
        if (count <= 1) return effective;

        // For each card compute its visual half-width in pixels (texture width * total scale / 2).
        var halfWidths = new List<float>(count);
        foreach (var c in cards)
        {
            float half = min_card_gap / 2.0f; // default half-width fallback
            if (c?.Card != null)
            {
                Sprite2D? sprite = c.Card.GetNodeOrNull<Sprite2D>("CardSprite");
                if (sprite == null)
                    sprite = c.Card.GetNodeOrNull<Sprite2D>("CardBorderSprite");

                if (sprite != null && sprite.Texture != null)
                {
                    var tex = sprite.Texture;
                    Vector2 texSize = tex.GetSize();
                    float scaleX = sprite.Scale.X != 0 ? sprite.Scale.X : (c.Card.Scale.X != 0 ? c.Card.Scale.X : 1.0f);
                    // Also include PlayableCard's own scale if present
                    scaleX *= c.Scale.X != 0 ? c.Scale.X : 1.0f;
                    half = (texSize.X * scaleX) / 2.0f;
                }
                else if (c.Card.image != null)
                {
                    var tex = c.Card.image;
                    Vector2 texSize = tex.GetSize();
                    float scaleX = c.Card.Scale.X != 0 ? c.Card.Scale.X : 1.0f;
                    scaleX *= c.Scale.X != 0 ? c.Scale.X : 1.0f;
                    half = (texSize.X * scaleX) / 2.0f;
                }
            }

            halfWidths.Add(half);
        }

        // Compute the maximum adjacent required centre distance so a constant
        // step between all cards satisfies the worst case and guarantees minimal overlap.
        float maxAdjacentNeeded = 0f;
        float maxFullWidth = 0f;
        for (int i = 0; i < halfWidths.Count - 1; i++)
        {
            float neededAdj = halfWidths[i] + halfWidths[i + 1] + min_card_padding;
            if (neededAdj > maxAdjacentNeeded) maxAdjacentNeeded = neededAdj;
        }
        // Also compute the maximum full card width seen (stronger no-overlap check)
        foreach (var h in halfWidths)
            if (h * 2.0f > maxFullWidth) maxFullWidth = h * 2.0f;

        // Baseline step coming from the configured arc width (span distributed evenly).
        float baselineStep = (2.0f * arc_width) / (count - 1);

        float baselineNeeded = Mathf.Max(maxAdjacentNeeded, baselineStep);
        if (force_no_overlap)
            baselineNeeded = Mathf.Max(baselineNeeded, maxFullWidth + min_card_padding);

        float step = baselineNeeded * spread_multiplier;
        // effectiveArcWidth is half of the total span: step * (count - 1) / 2
        float halfSpan = step * (count - 1) / 2.0f;
        return halfSpan;
    }

    private void AnimateCard(PlayableCard card, Vector2 targetPos, float targetRot)
    {
        bool isHovered = _hoveredCards.Contains(card);
        Vector2 liftOffset = isHovered ? new Vector2(0, -hover_lift) : Vector2.Zero;

        var tween = CreateTween().SetParallel();
        tween.TweenProperty(card, "position", targetPos + liftOffset, 0.15f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "rotation", targetRot, 0.15f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
    }

    // --- Hover handlers ---

    private void OnCardHovered(PlayableCard card)
    {
        _hoveredCards.Add(card);
        int i = cards.IndexOf(card);
        if (i < 0) return;
        float t = cards.Count == 1 ? 0f : Mathf.Lerp(-1f, 1f, (float)i / (cards.Count - 1));
        float effectiveArcWidth = ComputeEffectiveArcWidth(cards.Count);
        AnimateCard(card, ArcPosition(t, effectiveArcWidth), ArcRotation(t));
    }

    private void OnCardUnhovered(PlayableCard card)
    {
        _hoveredCards.Remove(card);
        int i = cards.IndexOf(card);
        if (i < 0) return;
        float t = cards.Count == 1 ? 0f : Mathf.Lerp(-1f, 1f, (float)i / (cards.Count - 1));
        float effectiveArcWidth = ComputeEffectiveArcWidth(cards.Count);
        AnimateCard(card, ArcPosition(t, effectiveArcWidth), ArcRotation(t));
    }
}
