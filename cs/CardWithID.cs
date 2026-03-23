using Godot;

public partial class CardWithID : Resource
{
    [Export] public int Id { get; set; }
    [Export] public CardData? Card { get; set; }

    public CardWithID() { }
    public CardWithID(int id, CardData? card)
    {
        Id = id;
        Card = card;
    }
}

