using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Hand : Control
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

    // --- Circular / fan layout properties (copied from the GDScript reference)
    [Export] public float hand_radius = 100.0f;
    [Export] public float card_angle = -90.0f;
    [Export] public float angle_limit = 20.0f;
    [Export] public float max_card_spread_angle = 5.0f;

    private HashSet<PlayableCard> _hoveredCards = new();
    public List<PlayableCard> cards = new();

    private HBoxContainer? _cardRow;
    private Dictionary<PlayableCard, Control> _slots = new();
    [Export] public int separation_override = -1;

    // The selected card is the hovered card with the highest index — pure derived state.
    private int SelectedCardIndex =>
        _hoveredCards.Count == 0
            ? -1
            : _hoveredCards.Max(c => cards.IndexOf(c));

    public override void _Ready()
    {
        _cardRow = GetNodeOrNull<HBoxContainer>("CardRow");

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

        // Create a slot to host the card so the HBoxContainer controls
        // horizontal layout while we can animate the card's rect_position
        // to implement hover lift.
        var slot = new Control();
        // Give the slot a minimal size so the HBoxContainer can size it.
        try { slot.RectMinSize = card.RectMinSize; } catch { }

        if (_cardRow != null)
            _cardRow.AddChild(slot);
        else
            AddChild(slot);

        slot.AddChild(card);
        card.RectPosition = Vector2.Zero;
        card.Visible = true;

        // Set slot minimum size from the card's RectMinSize (computed by Card)
        try
        {
            if (card.RectMinSize != Vector2.Zero)
                slot.RectMinSize = card.RectMinSize;
            else if (card.Card != null && card.Card.RectMinSize != Vector2.Zero)
                slot.RectMinSize = card.Card.RectMinSize;
        }
        catch { }

        _slots[card] = slot;

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
        if (_slots.TryGetValue(card, out var slot))
        {
            // Remove the slot (which also removes the card)
            if (slot.GetParent() is Node parent)
                parent.RemoveChild(slot);
            slot.QueueFree();
            _slots.Remove(card);
        }

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
        foreach (var card in removed)
        {
            if (_slots.TryGetValue(card, out var slot))
            {
                if (slot.GetParent() is Node parent)
                    parent.RemoveChild(slot);
                slot.QueueFree();
            }
        }
        _slots.Clear();
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
        if (count == 0)
        {
            if (_cardRow != null) _cardRow.Separation = 0;
            return;
        }

        float spacing = ComputeDesiredSpacing(count);
        if (_cardRow != null)
        {
            // If inspector override is set, use it; otherwise compute separation.
            if (separation_override >= 0)
                _cardRow.Separation = separation_override;
            else
            {
                // Compute average full width of cards (use Card.RectMinSize if available)
                float totalWidth = 0f;
                int measured = 0;
                foreach (var c in cards)
                {
                    try
                    {
                        if (c.Card != null && c.Card.RectMinSize != Vector2.Zero)
                        {
                            totalWidth += c.Card.RectMinSize.X;
                            measured++;
                        }
                    }
                    catch { }
                }
                float avgFullWidth = measured > 0 ? totalWidth / measured : 0f;
                // Convert center-to-center spacing into HBox separation (gap between child edges)
                int separation = 0;
                if (avgFullWidth > 0f)
                    separation = Mathf.Clamp((int)(spacing - avgFullWidth), 0, 1000);
                else
                    separation = Mathf.Clamp((int)spacing, 0, 1000);

                _cardRow.Separation = separation;

                // Ensure slot sizes reflect visual card sizes
                foreach (var kv in _slots)
                {
                    var card = kv.Key;
                    var slot = kv.Value;
                    try
                    {
                        if (card.Card != null && card.Card.RectMinSize != Vector2.Zero)
                            slot.RectMinSize = card.Card.RectMinSize;
                    }
                    catch { }
                }
            }
        }
    }

    /// <summary>
    /// Compute a sensible spacing (centre-to-centre) between adjacent cards.
    /// Attempts to read card sprite sizes similarly to ComputeEffectiveArcWidth,
    /// but returns a spacing value rather than an arc width.
    /// </summary>
    private float ComputeDesiredSpacing(int count)
    {
        if (count <= 1) return 0.0f;

        // For each card compute its visual half-width in pixels (texture width * total scale / 2).
        var halfWidths = new List<float>(count);
        foreach (var c in cards)
        {
            float half = min_card_gap / 2.0f; // default half-width fallback
            if (c?.Card != null)
            {
                // Prefer using RectMinSize if already computed on the card control
                try
                {
                    if (c.RectMinSize != Vector2.Zero)
                    {
                        half = c.RectMinSize.X / 2.0f;
                    }
                    else
                    {
                        var sprite = c.Card?.GetNodeOrNull<TextureRect>("CardSprite");
                        if (sprite != null && sprite.Texture != null)
                        {
                            var tex = sprite.Texture;
                            Vector2 texSize = tex.GetSize();
                            float scaleX = sprite.RectScale.X != 0 ? sprite.RectScale.X : (c.Card.RectScale.X != 0 ? c.Card.RectScale.X : 1.0f);
                            scaleX *= c.RectScale.X != 0 ? c.RectScale.X : 1.0f;
                            half = (texSize.X * scaleX) / 2.0f;
                        }
                        else if (c.Card.image != null)
                        {
                            var tex = c.Card.image;
                            Vector2 texSize = tex.GetSize();
                            float scaleX = c.Card.RectScale.X != 0 ? c.Card.RectScale.X : 1.0f;
                            scaleX *= c.RectScale.X != 0 ? c.RectScale.X : 1.0f;
                            half = (texSize.X * scaleX) / 2.0f;
                        }
                    }
                }
                catch { }
            }

            halfWidths.Add(half);
        }

        float maxAdjacentNeeded = 0f;
        float maxFullWidth = 0f;
        for (int i = 0; i < halfWidths.Count - 1; i++)
        {
            float neededAdj = halfWidths[i] + halfWidths[i + 1] + min_card_padding;
            if (neededAdj > maxAdjacentNeeded) maxAdjacentNeeded = neededAdj;
        }
        foreach (var h in halfWidths)
            if (h * 2.0f > maxFullWidth) maxFullWidth = h * 2.0f;

        float baselineNeeded = Mathf.Max(maxAdjacentNeeded, min_card_gap);
        if (force_no_overlap)
            baselineNeeded = Mathf.Max(baselineNeeded, maxFullWidth + min_card_padding);

        return baselineNeeded;
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
                // Prefer RectMinSize computed on the Card control
                try
                {
                    if (c.Card.RectMinSize != Vector2.Zero)
                    {
                        half = c.Card.RectMinSize.X / 2.0f;
                    }
                    else
                    {
                        var sprite = c.Card.GetNodeOrNull<TextureRect>("CardSprite");
                        if (sprite != null && sprite.Texture != null)
                        {
                            var tex = sprite.Texture;
                            Vector2 texSize = tex.GetSize();
                            float scaleX = sprite.RectScale.X != 0 ? sprite.RectScale.X : (c.Card.RectScale.X != 0 ? c.Card.RectScale.X : 1.0f);
                            // Also include PlayableCard's own rect scale if present
                            scaleX *= c.RectScale.X != 0 ? c.RectScale.X : 1.0f;
                            half = (texSize.X * scaleX) / 2.0f;
                        }
                        else if (c.Card.image != null)
                        {
                            var tex = c.Card.image;
                            Vector2 texSize = tex.GetSize();
                            float scaleX = c.Card.RectScale.X != 0 ? c.Card.RectScale.X : 1.0f;
                            scaleX *= c.RectScale.X != 0 ? c.RectScale.X : 1.0f;
                            half = (texSize.X * scaleX) / 2.0f;
                        }
                    }
                }
                catch { }
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
        // Animate the PlayableCard control's rect_position (local to its parent slot/HBox)
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(card, "rect_position", targetPos, 0.15f)
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

        // Lift the card relative to its current rect_position
        AnimateCard(card, new Vector2(0, -hover_lift), 0.0f);
    }

    private void OnCardUnhovered(PlayableCard card)
    {
        _hoveredCards.Remove(card);
        int i = cards.IndexOf(card);
        if (i < 0) return;

        AnimateCard(card, Vector2.Zero, 0.0f);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);


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
}
