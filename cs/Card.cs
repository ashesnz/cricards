using Godot;
using System;

[Tool]
public partial class Card : Node2D
{
	[Signal] public delegate void MouseEnteredEventHandler(Card card);
	[Signal] public delegate void MouseExitedEventHandler(Card card);

	public enum Colour { RED, BLUE, GREEN, PURPLE }

	[Export] public CardData? card_data;

	public string title = "TODO: Card Title";
	public string description = "TODO: Card Description";
	public int cost = 1;
	public Texture2D? image;
	public Colour colour = Colour.RED;
	public CardData.Type type = CardData.Type.ATTACK;

	private bool is_highlighted = false;
	private Tween? tween_unhighlight;

	private Vector2 _original_scale;
	private Vector2 _original_position;

	private Sprite2D? card_border_sprite;
	private Sprite2D? image_sprite;
	private Label? cost_label;
	private Label? title_label;
	private Label? description_label;
	private Label? type_label;
	private Area2D? area_2d;
	private Sprite2D? card_sprite;

	public override void _Ready()
	{
		card_border_sprite = GetNodeOrNull<Sprite2D>("CardBorderSprite");
		image_sprite = GetNodeOrNull<Sprite2D>("ImageSprite");
		cost_label = GetNodeOrNull<Label>("CostLabel");
		title_label = GetNodeOrNull<Label>("TitleLabel");
		description_label = GetNodeOrNull<Label>("descriptionLabel");
		type_label = GetNodeOrNull<Label>("TypeLabel");
		area_2d = GetNodeOrNull<Area2D>("Area2D");
		card_sprite = GetNodeOrNull<Sprite2D>("CardSprite");

		if (area_2d != null)
		{
			area_2d.MouseEntered += _on_area_2d_mouse_entered;
			area_2d.MouseExited += _on_area_2d_mouse_exited;
		}
	}

	public override void _Process(double delta)
	{
		_UpdateGraphics();
	}

	public void SetValues(string? temp_title = null, string? temp_description = null, int temp_cost = 1, CardData.Type temp_type = CardData.Type.ATTACK, Texture2D? temp_image = null)
	{
		title = temp_title ?? title;
		description = temp_description ?? description;
		cost = temp_cost;
		_SetColour(temp_type);
		_SetType(temp_type);
		if (temp_image != null)
			_SetImage(temp_image);
		else if (card_data != null)
			_SetImage(card_data.Image);

		_original_scale = Scale;
		_original_position = Position;
	}

	public void Highlight()
	{
		if (!is_highlighted)
		{
			is_highlighted = true;
			var tween = CreateTween();
			tween.SetParallel();
			tween.TweenProperty(this, "scale", _original_scale * 1.25f, 0.2f);
			tween.TweenProperty(this, "position:y", _original_position.Y - 135, 0.2f);
		}
	}

	public void Unhighlight()
	{
		if (is_highlighted)
		{
			is_highlighted = false;
			tween_unhighlight = CreateTween();
			tween_unhighlight.SetParallel();
			tween_unhighlight.TweenProperty(this, "scale", _original_scale * 1.0f, 0.5f);
			tween_unhighlight.TweenProperty(this, "position:y", _original_position.Y, 0.5f);
		}
	}

	private void _SetColour(CardData.Type temp_type)
	{
		if (card_border_sprite == null)
			return;

		switch (temp_type)
		{
			case CardData.Type.ATTACK:
				card_border_sprite.Modulate = _GetColour(Colour.RED);
				break;
			case CardData.Type.DEFENSE:
				card_border_sprite.Modulate = _GetColour(Colour.BLUE);
				break;
			case CardData.Type.SKILL:
				card_border_sprite.Modulate = _GetColour(Colour.GREEN);
				break;
			case CardData.Type.SECRET:
				card_border_sprite.Modulate = _GetColour(Colour.PURPLE);
				break;
		}
	}

	private Color _GetColour(Colour colour)
	{
		switch (colour)
		{
			case Colour.RED:
				return new Color("#ff6b6b");
			case Colour.BLUE:
				return new Color("#5ac8fa");
			case Colour.GREEN:
				return new Color("#5dc89d");
			case Colour.PURPLE:
				return new Color("#aa8cee");
		}
		return Colors.White;
	}

	private void _SetImage(Texture2D texture)
	{
		image = texture;
		if (image_sprite != null)
			image_sprite.Texture = image;
	}

	private string _SetType(CardData.Type card_data_type)
	{
		type = card_data_type;
		switch (card_data_type)
		{
			case CardData.Type.ATTACK:
				return "Attack";
			case CardData.Type.DEFENSE:
				return "Defense";
			case CardData.Type.SKILL:
				return "Skill";
			default:
				return "";
		}
	}

	private void _UpdateGraphics()
	{
		if (card_data != null)
			SetValues(card_data.Title, card_data.Description, card_data.Cost, card_data.CardType, card_data.Image);

		if (cost_label != null && cost_label.Text != cost.ToString())
			cost_label.Text = cost.ToString();

		if (title_label != null && title_label.Text != title)
			title_label.Text = title;

		if (description_label != null && description_label.Text != description)
			description_label.Text = description;

		if (type_label != null && type_label.Text != _SetType(type))
			type_label.Text = _SetType(type);

		_SetColour(type);

		if (image != null && image_sprite != null)
			image_sprite.Texture = image;
	}

	private void _on_area_2d_mouse_entered()
	{
		EmitSignal(SignalName.MouseEntered, this);
	}

	private void _on_area_2d_mouse_exited()
	{
		EmitSignal(SignalName.MouseExited, this);
	}
}


