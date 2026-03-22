
using Godot;
using Godot.Collections;

[Tool]
public partial class SecrecyBar : ProgressBar
{
	private AudioStreamPlayer secret_full_sfx;
	private AudioStreamPlayer secret_update_sfx;

	public override void _Ready()
	{
		secret_full_sfx = GetNodeOrNull<AudioStreamPlayer>("SecretBarFullSFX");
		secret_update_sfx = GetNodeOrNull<AudioStreamPlayer>("SecretUpdateSFX");
	}

	// Initialize the bar for the given character data (sets max and resets value)
	public void Initialize(CharacterData characterData)
	{
		if (characterData != null)
			MaxValue = characterData.NumSecrets;
		Value = 0.0;
	}

	// Update (increment) the bar by `num` secrets revealed
	public void Update(int num)
	{
		Value += num;
		if (secret_update_sfx != null) secret_update_sfx.Play();
		if (IsSecretRevealed())
		{
			if (secret_full_sfx != null) secret_full_sfx.Play();
		}
	}

	public void Restart()
	{
		Value = 0.0;
	}

	public bool IsSecretRevealed()
	{
		return Value >= MaxValue;
	}
}

