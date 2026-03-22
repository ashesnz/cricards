using System;
using Godot;
using Godot.Collections;

public partial class Action : RefCounted
{
    public Character? actor;
    public int cost;

    // New strongly-typed overload using ActionContext
    public virtual void Activate(ActionContext ctx)
    {
        if (ctx == null)
            return;

        if (ctx.TryGetAs<Character>("actor", out var a))
            actor = a;

        cost = ctx.GetIntOrDefault("cost", 0);
    }

    // Backwards-compatible shim for existing callers that pass a raw Dictionary
    public virtual void Activate(Dictionary game_state)
    {
        if (game_state == null)
            return;

        Activate(new ActionContext(game_state));
    }

    // Helper to convert various Godot Variant-backed values to int safely.
    private static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;
        if (value == null)
            return false;

        switch (value)
        {
            case int i:
                result = i;
                return true;
            case long l:
                result = (int)l;
                return true;
            case float f:
                result = (int)f;
                return true;
            case double d:
                result = (int)d;
                return true;
            case string s when int.TryParse(s, out var parsed):
                result = parsed;
                return true;
            default:
                try
                {
                    result = Convert.ToInt32(value);
                    return true;
                }
                catch
                {
                    return false;
                }
        }
    }
}

