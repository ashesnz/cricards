using Godot;
using System;

[Tool]
public partial class Character : Node2D
{
	[Export] public Resource? character_data;

	public CharacterData? GetCharacterData()
	{
		return character_data as CharacterData;
	}

	public int health = 5;
	public int mana = 5;
	public int defense = 0;
	public int number_of_cards_to_be_dealt = 5;
	public int num_secrets = 3;

	private Sprite2D? defense_icon;
	private ProgressBar? health_bar;
	private Label? label;
	private Sprite2D? sprite_2d;
	private Node2D? pivot;
	private Label? health_bar_label;

	public override void _Ready()
	{
		defense_icon = GetNodeOrNull<Sprite2D>("DefenseIcon");
		health_bar = GetNodeOrNull<ProgressBar>("HealthBar");
		label = GetNodeOrNull<Label>("DefenseIcon/Label");
		sprite_2d = GetNodeOrNull<Sprite2D>("Pivot/Sprite2D");
		pivot = GetNodeOrNull<Node2D>("Pivot");
		health_bar_label = GetNodeOrNull<Label>("HealthBar/HealthBarLabel");

		HardReset();
	}

	public override void _Process(double delta)
	{
		var cd = GetCharacterData();
		if (sprite_2d != null && sprite_2d.Texture != null && cd != null && cd.Texture != null)
		{
			sprite_2d.Texture = cd.Texture;
		}

		if (sprite_2d != null && pivot != null && sprite_2d.Texture != null)
		{
			sprite_2d.Offset = new Vector2(sprite_2d.Offset.X, pivot.Position.Y - (sprite_2d.Position.Y + sprite_2d.Texture.GetHeight() / 2.0f) * 0.95f);
		}

		UpdateHealthBar();
		UpdateDefenseIcon();
	}

	public void LoadData(CharacterData data)
	{
		character_data = data;
		HardReset();
	}

	public void SetHealthValues(int new_health, int new_max_health)
	{
		var cd3 = GetCharacterData();
		if (cd3 != null)
			cd3.MaxHealth = new_max_health;
		health = new_health;
	}

	public void StartTurn()
	{
		defense = 0;
		var cd4 = GetCharacterData();
		if (cd4 != null)
			mana = cd4.CurrentManaCap;
	}

	public void UpdateHealthBar()
	{
		if (health_bar == null)
		{
			GD.Print("Character.cs => Error: health bar is null");
			return;
		}

		var cd2 = GetCharacterData();
		if (cd2 != null && Math.Abs(health_bar.MaxValue - cd2.MaxHealth) > 0.001f)
			health_bar.MaxValue = cd2.MaxHealth;
		if (health_bar.Value != health)
			health_bar.Value = health;

		if (health_bar_label != null && cd2 != null)
			health_bar_label.Text = ((int)health_bar.Value).ToString() + "/" + cd2.MaxHealth.ToString();
	}

	public void UpdateDefenseIcon()
	{
		if (defense_icon == null || label == null)
		{
			GD.Print("Character.cs => Error: defense icon or label is null");
			return;
		}

		defense_icon.Visible = defense > 0;
		label.Text = defense.ToString();
	}

	public void SpendMana(int amount)
	{
		mana -= amount;
	}

	public Tween DealDamageAnimation()
	{
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetParallel(true);
		if (sprite_2d != null)
		{
			tween.TweenProperty(sprite_2d, "rotation", 0.1f, 0.2f);
			tween.TweenProperty(sprite_2d, "scale", new Vector2(1.2f, 0.8f), 0.2f);
		}

		tween.SetParallel(false);
		if (sprite_2d != null)
		{
			tween.TweenProperty(sprite_2d, "rotation", 0.0f, 0.2f);
			tween.TweenProperty(sprite_2d, "scale", new Vector2(1.0f, 1.0f), 0.2f);
		}

		return tween;
	}

	public void TakeDamage(int amount)
	{
		var temp = amount;
		amount = Math.Max(amount - defense, 0);
		defense = Math.Max(defense - temp, 0);
		health -= amount;

		UpdateHealthBar();
		UpdateDefenseIcon();
		TakeDamageAnimation(amount);
	}

	public void TakeDamageAnimation(int damage)
	{
		if (health <= 0)
		{
			var tween = CreateTween();
			tween.TweenProperty(this, "modulate:a", 0.0f, 1.0f);
		}
		else if (damage > 0 && sprite_2d != null)
		{
			var tween = CreateTween();
			tween.TweenProperty(sprite_2d, "modulate", new Color(1.0f, 0.8f, 0.8f, 1.0f), 0.1f);
			tween.TweenProperty(sprite_2d, "modulate", Colors.White, 0.1f);

			var tween1 = CreateTween();
			tween1.TweenProperty(sprite_2d, "rotation", 0.05f, 0.1f);
			tween1.TweenProperty(sprite_2d, "rotation", -0.05f, 0.2f);
			tween1.TweenProperty(sprite_2d, "rotation", 0.0f, 0.1f);
		}
	}

	public void AddDefense(int amount)
	{
		defense += amount;
		UpdateDefenseIcon();
	}

	public void HealUpALittle()
	{
		var cd5 = GetCharacterData();
		if (cd5 == null)
			return;
		var diff = cd5.MaxHealth - health;
		var rng = new RandomNumberGenerator();
		rng.Randomize();
		var random_heals = (int)rng.RandiRange(1, Math.Max(1, diff));
		health += random_heals;
	}

	public void HardReset()
	{
		var cd6 = GetCharacterData();
		if (cd6 != null && !string.IsNullOrEmpty(cd6.Name))
			Name = cd6.Name;
		if (cd6 != null)
			health = cd6.MaxHealth;
		if (cd6 != null)
			mana = cd6.StartMana;
		if (cd6 != null)
			defense = cd6.BaseDefense;
		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f);

		UpdateHealthBar();
		UpdateDefenseIcon();
	}

	public void SoftReset()
	{
		var cd = GetCharacterData();
		if (cd != null && !string.IsNullOrEmpty(cd.Name))
			Name = cd.Name;
		if (cd != null)
			mana = cd.StartMana;
		if (cd != null)
			defense = cd.BaseDefense;
		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f);

		UpdateHealthBar();
		UpdateDefenseIcon();
	}
}


