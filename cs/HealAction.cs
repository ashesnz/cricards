using Godot;
using Godot.Collections;

public partial class HealAction : Action
{
    public int num_heal = 1;

    public override void Activate(ActionContext ctx)
    {
        base.Activate(ctx);
        if (actor != null) actor.health += num_heal;
    }
}

