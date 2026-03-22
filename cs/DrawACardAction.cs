using Godot;
using Godot.Collections;

public partial class DrawACardAction : Action
{
	// How many cards to draw when this action is processed by game flow
	public int number_of_cards_to_draw = 1;

	public override void Activate(ActionContext ctx)
	{
		// Keep default activation behavior (populate actor/cost)
		base.Activate(ctx);
		// Actual drawing of cards is handled by Main game flow which has access
		// to the hand/draw piles. This class only carries the parameter.
	}
}


