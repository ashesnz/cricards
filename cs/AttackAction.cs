using Godot;
using Godot.Collections;

public partial class AttackAction : Action
{
    public override void Activate(ActionContext ctx)
    {
        base.Activate(ctx);
        actor?.DealDamageAnimation();

        foreach (var target in ctx.GetArrayOf<Character>("targets"))
            target.TakeDamage(1);
    }
}

