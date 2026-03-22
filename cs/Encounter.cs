using Godot;

[Tool]
public partial class Encounter : TextureButton
{
    [Signal] public delegate void ChosenEventHandler(Encounter encounter);

    // Export as a generic Resource to avoid engine cast errors when the C# type
    // isn't yet available during project reload. Use GetCharacterData() to
    // access the strongly-typed CharacterData when available.
    [Export] public Resource? character_data;
    [Export] public Texture2D? location;
    [Export]
    public Godot.Collections.Array connections { get; set; } = new Godot.Collections.Array();

    private Label? label;

    public override void _Ready()
    {
        label = GetNodeOrNull<Label>("Label");
        var cd = GetCharacterData();
        if (label != null && cd != null)
            label.Text = cd.Name;
        Pressed += _OnPressed;
    }

    public override void _Process(double delta)
    {
        var cd2 = GetCharacterData();
        if (Engine.IsEditorHint() && label != null && cd2 != null)
            label.Text = cd2.Name;

    }

    // Convenience accessor to get the strongly-typed CharacterData when available
    public CharacterData? GetCharacterData()
    {
        return character_data as CharacterData;
    }

    public Vector2 GetCenterPosition()
    {
        var position = GlobalPosition;
        if (TextureNormal != null)
        {
            position.X += TextureNormal.GetWidth() / 2.0f;
            position.Y += TextureNormal.GetHeight() / 2.0f;
        }
        return position;
    }

    private void _OnPressed()
    {
        EmitSignal(SignalName.Chosen, this);
    }
}

