using Godot;
using Godot.Collections;

public partial class Rewards : Control
{
	[Signal] public delegate void ChosenEventHandler(PlayableCard playable_card);

	public int num_rewards = 1;

	private RewardChooseACard? reward_choose_a_card;
	private TextureRect? rewards_panel;
	private Button? choose_a_card_button;
	private Button? choose_a_secret_button;

	public override void _Ready()
	{
		reward_choose_a_card = GetNodeOrNull<RewardChooseACard>("RewardChooseACard");
		rewards_panel = GetNodeOrNull<TextureRect>("RewardsPanel");
		choose_a_card_button = GetNodeOrNull<Button>("ChooseACardButton");
		choose_a_secret_button = GetNodeOrNull<Button>("ChooseASecretButton");

		if (choose_a_card_button != null)
			choose_a_card_button.Pressed += _OnChooseACardButtonPressed;
		if (choose_a_secret_button != null)
			choose_a_secret_button.Pressed += _OnChooseASecretButtonPressed;
		if (reward_choose_a_card != null)
			reward_choose_a_card.Chosen += _OnChosen;
	}

	public void Activate(bool is_revealed_secret)
	{
		Visible = true;
		if (rewards_panel != null) rewards_panel.Visible = true;
		if (choose_a_card_button != null) choose_a_card_button.Disabled = false;
		if (choose_a_secret_button != null) choose_a_secret_button.Disabled = false;
		if (choose_a_secret_button != null) choose_a_secret_button.Visible = is_revealed_secret;

		num_rewards = is_revealed_secret ? 2 : 1;
	}

	private void _OnChooseACardButtonPressed()
	{
		if (choose_a_card_button != null) choose_a_card_button.Disabled = true;
		if (rewards_panel != null) rewards_panel.Visible = false;
		if (reward_choose_a_card != null)
		{
			reward_choose_a_card.Visible = true;
			reward_choose_a_card.Activate(false);
		}
	}

	private void _OnChooseASecretButtonPressed()
	{
		if (choose_a_secret_button != null) choose_a_secret_button.Disabled = true;
		if (rewards_panel != null) rewards_panel.Visible = false;
		if (reward_choose_a_card != null)
		{
			reward_choose_a_card.Visible = true;
			reward_choose_a_card.Activate(true);
		}
	}

	private void _OnChosen(PlayableCard playable_card)
	{
		EmitSignal(SignalName.Chosen, playable_card);
		if (rewards_panel != null) rewards_panel.Visible = true;
	}
}


