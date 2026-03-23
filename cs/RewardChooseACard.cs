using Godot;
using Godot.Collections;

public partial class RewardChooseACard : MarginContainer
{
    [Signal] public delegate void ChosenEventHandler(PlayableCard playable_card);

    [Export] public Array normal_possible_rewards = new Array(); // CardData
    [Export] public Array secret_possible_rewards = new Array(); // CardData
    [Export] public CardData? test_card_data;

    private Array _reward_card_containers = new Array(); // RewardCardContainer
    private Array _chosen_rewards = new Array();

    private HBoxContainer? h_box_container;
    private RewardCardContainer? reward_card_container_1;
    private RewardCardContainer? reward_card_container_2;
    private RewardCardContainer? reward_card_container_3;
    private Button? skip_button;

    public override void _Ready()
    {
        skip_button = GetNodeOrNull<Button>("VBoxContainer/SkipButton");
        if (skip_button != null)
            skip_button.Pressed += () => _OnChosen(null);

        reward_card_container_1 = GetNodeOrNull<RewardCardContainer>("VBoxContainer/HBoxContainer/RewardCardContainer1");
        reward_card_container_2 = GetNodeOrNull<RewardCardContainer>("VBoxContainer/HBoxContainer/RewardCardContainer2");
        reward_card_container_3 = GetNodeOrNull<RewardCardContainer>("VBoxContainer/HBoxContainer/RewardCardContainer3");

        if (reward_card_container_1 != null) _reward_card_containers.Add(reward_card_container_1);
        if (reward_card_container_2 != null) _reward_card_containers.Add(reward_card_container_2);
        if (reward_card_container_3 != null) _reward_card_containers.Add(reward_card_container_3);
    }

    public void Activate(bool is_revealed_secret)
    {
        Visible = true;
        _chosen_rewards.Clear();
        foreach (object obj in _reward_card_containers)
        {
            var container = obj as RewardCardContainer;
            if (container == null) continue;
            var reward = _GetReward(is_revealed_secret);
            if (reward != null && container.PlayableCard != null)
                container.PlayableCard.LoadCardData(reward);
            container.Chosen += _OnChosen;
        }
    }

    private void _OnChosen(PlayableCard? playable_card)
    {
        if (playable_card != null)
            EmitSignal(SignalName.Chosen, (GodotObject)playable_card);
        else
            EmitSignal(SignalName.Chosen, new Variant());
        Visible = false;
    }

    private CardData _GetReward(bool is_revealed_secret)
    {
        CardData? reward = null;
        while (reward == null)
        {
            if (is_revealed_secret)
            {
                object o = secret_possible_rewards.PickRandom();
                reward = o as CardData;
            }
            else
            {
                object o = normal_possible_rewards.PickRandom();
                reward = o as CardData;
            }

            if (reward == null)
                continue;

            if (!_chosen_rewards.Contains(reward))
                _chosen_rewards.Add(reward);
            else
                reward = null;
        }
        return reward!;
    }
}

