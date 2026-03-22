using System;
using System.Collections.Generic;
using Godot.Collections;

public sealed class ActionContext
{
    public Dictionary GameState { get; }

    public ActionContext(Dictionary gameState)
    {
        GameState = gameState ?? new Dictionary();
    }

    // Lazy accessors that delegate to DictionaryExtensions
    public T? GetAs<T>(string key) where T : class
    {
        if (GameState.TryGetAs<T>(key, out var v))
            return v;
        return null;
    }

    public bool TryGetAs<T>(string key, out T? value) where T : class
    {
        return GameState.TryGetAs<T>(key, out value);
    }

    public int GetIntOrDefault(string key, int defaultValue = 0)
    {
        return GameState.GetIntOrDefault(key, defaultValue);
    }

    public IEnumerable<T> GetArrayOf<T>(string key) where T : class
    {
        if (!GameState.ContainsKey(key))
            yield break;

        var raw = GameState[key];
        object boxed = raw;
        if (boxed is Godot.Collections.Array arr)
        {
            foreach (var item in arr.OfType<T>())
                yield return item;
        }
    }
}

