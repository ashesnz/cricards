using Godot;
using Godot.Collections;

public partial class ChoiceRemoveCards : Control
{
    [Signal] public delegate void ChosenEventHandler(PlayableCard? playable_card);

    [Export] public CardData? test_card_data;

    private Array _card_containers = new Array(); // RewardCardContainer
    private CardData? _chosen_card;

    private HBoxContainer? h_box_container;
    private RewardCardContainer? remove_card_container_1;
    private RewardCardContainer? remove_card_container_2;
    private RewardCardContainer? remove_card_container_3;
    private Button? skip_button;

    public override void _Ready()
    {
        skip_button = GetNodeOrNull<Button>("SkipButton");
        if (skip_button != null)
            skip_button.Pressed += () => _OnChosen(null);

        remove_card_container_1 = GetNodeOrNull<RewardCardContainer>("RewardCardContainer1");
        remove_card_container_2 = GetNodeOrNull<RewardCardContainer>("RewardCardContainer2");
        remove_card_container_3 = GetNodeOrNull<RewardCardContainer>("RewardCardContainer3");

        if (remove_card_container_1 != null) _card_containers.Add(remove_card_container_1);
        if (remove_card_container_2 != null) _card_containers.Add(remove_card_container_2);
        if (remove_card_container_3 != null) _card_containers.Add(remove_card_container_3);
    }

    public void Activate(Deck deck)
    {
        Visible = true;
        _chosen_card = null;
        foreach (object obj in _card_containers)
        {
            var card_container = obj as RewardCardContainer;
            if (card_container == null) continue;
            var random = deck.GetPlayableDeck().GetRandomCard();
            var card_data = random != null ? random.Card : null;
            if (card_data != null && card_container.PlayableCard != null)
                card_container.PlayableCard.LoadCardData(card_data);
            card_container.Chosen += _OnChosen;
        }
    }

    private void _OnChosen(PlayableCard? playable_card)
    {
        if (playable_card != null && playable_card.card_data != null)
            GD.Print("emitting: " + playable_card.card_data.Title);
        else
            GD.Print("emitting null");

        EmitSignal(SignalName.Chosen, (GodotObject)playable_card);
        Visible = false;
    }
}

