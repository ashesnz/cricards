using Godot;
using Godot.Collections;

public partial class ExhaustOtherRandomAction : Action
{
    public override void Activate(ActionContext ctx)
    {
        base.Activate(ctx);
        // Implementation will be handled by game flow (Main/Hand) where random card selection occurs
    }
}

