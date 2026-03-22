using Godot;

public partial class CharacterData : Resource
{
    [Export] public string Name { get; set; }
    [Export] public Texture2D Texture { get; set; }
    [Export] public int MaxHealth { get; set; }
    [Export] public int StartMana { get; set; }
    [Export] public int BaseDefense { get; set; }
    [Export] public int CurrentManaCap { get; set; }
    [Export] public int NumSecrets { get; set; }
}

