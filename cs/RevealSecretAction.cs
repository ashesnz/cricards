using Godot;
using Godot.Collections;

public partial class RevealSecretAction : Action
{
	// Number of secrets to reveal
	public int num_secrets_revealed = 1;

	public override void Activate(ActionContext ctx)
	{
		base.Activate(ctx);
		// The Main game flow handles updating the SecrecyBar when it detects this action type.
	}
}


