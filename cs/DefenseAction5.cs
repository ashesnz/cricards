using Godot;
using Godot.Collections;

public partial class DefenseAction5 : Action
{
    public override void Activate(ActionContext ctx)
    {
        base.Activate(ctx);
        actor?.AddDefense(5);
    }
}

