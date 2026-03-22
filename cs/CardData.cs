using Godot;
using System;
using Godot.Collections;

[Tool]
public partial class CardData : Resource
{
    public enum Type { ATTACK, DEFENSE, SKILL, SECRET }

    [Export] public string Title { get; set; }
    [Export] public string Description { get; set; }
    [Export] public int Cost { get; set; }
    [Export] public CompressedTexture2D? Image { get; set; }
    // initialize defaults
    public CardData()
    {
        Title = string.Empty;
        Description = string.Empty;
        Image = null;
    }
    [Export] public Type CardType { get; set; }
    // Explicitly use Godot.Collections.Array to avoid ambiguity with System.Array
    [Export] public Godot.Collections.Array Actions { get; set; } = new Godot.Collections.Array(); // scripts (Script resources)

    public Card.Colour Colour;
}

