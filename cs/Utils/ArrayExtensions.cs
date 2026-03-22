using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class ArrayExtensions
{
    // Enumerate items of type T from a Godot Array, performing safe casts.
    public static IEnumerable<T> OfType<T>(this Array arr) where T : class
    {
        if (arr == null)
            yield break;

        foreach (var raw in arr)
        {
            // Box the Variant-backed value into object so the C# compiler can perform pattern matching
            object boxed = raw;

            if (boxed is T t)
            {
                yield return t;
                continue;
            }

            // Some Godot values may be RefCounted wrappers that can be cast at runtime
            var rc = boxed as RefCounted;
            if (rc is T t2)
            {
                yield return t2;
                continue;
            }
        }
    }
}

