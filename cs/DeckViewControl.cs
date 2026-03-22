using Godot;

public partial class DeckViewControl : Control
{
    public enum Type { DRAW_PILE, DISCARD_PILE, DECK }
    public Type current_type;

    private DeckViewWindow deck_view_window;
    private Button back_button;
    private Label title_label;
    private Label description_label;

    // Expose the DeckViewWindow instance for external callers (Main) to display lists
    public DeckViewWindow DeckViewWindow => deck_view_window;

    public override void _Ready()
    {
        deck_view_window = GetNodeOrNull<DeckViewWindow>("DeckViewWindow");
        back_button = GetNodeOrNull<Button>("BackButton");
        title_label = GetNodeOrNull<Label>("TitleLabel");
        description_label = GetNodeOrNull<Label>("DescriptionLabel");

        if (back_button != null)
            back_button.Pressed += _on_back_button_pressed;
    }

    // Public wrapper so external callers (e.g., Main) can trigger the back action.
    public void Back()
    {
        _on_back_button_pressed();
    }

    public void PlayAudio(Type type, bool is_open)
    {
        if (is_open)
        {
            switch (type)
            {
                case Type.DRAW_PILE:
                    // play draw_deck_open
                    break;
                case Type.DISCARD_PILE:
                    // play discard_deck_open
                    break;
                case Type.DECK:
                    // play draw_deck_open
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case Type.DRAW_PILE:
                    // play draw_deck_close
                    break;
                case Type.DISCARD_PILE:
                    // play draw_deck_close
                    break;
                case Type.DECK:
                    // play draw_deck_close
                    break;
            }
        }
    }

    public void SetType(Type type)
    {
        current_type = type;
        _SetDescription(type);
        _SetTitle(type);
    }

    private void _on_back_button_pressed()
    {
        Visible = !Visible;
        PlayAudio(current_type, Visible);
    }

    private void _SetTitle(Type type)
    {
        switch (type)
        {
            case Type.DRAW_PILE:
                title_label.Text = "Draw Pile";
                break;
            case Type.DISCARD_PILE:
                title_label.Text = "Discard Pile";
                break;
            case Type.DECK:
                title_label.Text = "The Deck";
                break;
        }
    }

    private void _SetDescription(Type type)
    {
        switch (type)
        {
            case Type.DRAW_PILE:
                description_label.Text = "Cards are drawn from here at the start of each turn.";
                break;
            case Type.DISCARD_PILE:
                description_label.Text = "Cards shuffled into your empty draw pile.";
                break;
            case Type.DECK:
                description_label.Text = "Cards you start with, each encounter.";
                break;
        }
    }
}

