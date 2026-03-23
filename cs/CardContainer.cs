using Godot;
using Godot.Collections;

public partial class CardContainer : Container
{
	private const float SCALE = 0.95f;
	private static readonly Vector2 CARD_COMPONENT_POSITION = new Vector2(102, 123);

	[Export] public PackedScene? playable_card_scene;

	public PlayableCard? PlayableCard { get; private set; }

	private CardData? _card;
	public CardData? card
	{
		get => _card;
		set
		{
			_card = value;
			if (playable_card_scene != null)
			{
				PlayableCard = playable_card_scene.Instantiate() as PlayableCard;
				if (PlayableCard != null)
				{
					AddChild(PlayableCard);
								PlayableCard.Position = CARD_COMPONENT_POSITION;
					if (_card != null)
						PlayableCard.LoadCardData(_card);
								PlayableCard.Scale = Vector2.One * SCALE;
				}
			}
		}
	}
}


