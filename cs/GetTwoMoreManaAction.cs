using Godot;
using Godot.Collections;

public partial class GetTwoMoreManaAction : Action
{
    public override void Activate(ActionContext ctx)
    {
        base.Activate(ctx);
        if (actor != null) actor.mana += 2;
    }
}

