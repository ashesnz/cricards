using Godot;
using Godot.Collections;

public partial class DefenseAction1 : Action
{
    public override void Activate(ActionContext ctx)
    {
        base.Activate(ctx);
        actor?.AddDefense(1);
    }
}

