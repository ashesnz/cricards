using Godot;
using Godot.Collections;

public partial class ExhaustAction : Action
{
	public override void Activate(ActionContext ctx)
	{
		base.Activate(ctx);
		// The Main game flow sets the PlayableCard.exhausted flag when it encounters this action.
	}
}


