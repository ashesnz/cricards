using System;
using Godot.Collections;

public static class DictionaryExtensions
{
    // Optional hook for logging conversion failures. Assign a delegate to capture messages.
    public static Action<string>? ConversionLogger;

    // Try to get a reference-typed value of T from the Godot Dictionary.
    // This boxes the Variant into object once and does a safe 'is' check.
    public static bool TryGetAs<T>(this Dictionary dict, string key, out T? value) where T : class
    {
        value = null;
        if (dict == null || !dict.ContainsKey(key))
            return false;

        var raw = dict[key];
        object boxed = raw;
        if (boxed is T t)
        {
            value = t;
            return true;
        }
        // conversion failed - log if requested
        ConversionLogger?.Invoke($"DictionaryExtensions.TryGetAs<{typeof(T).Name}> failed for key '{key}', runtime type: {boxed?.GetType().Name ?? "null"}");
        return false;
    }

    // Try to convert a Dictionary entry to int (handles int/long/float/double/string/others via Convert).
    public static bool TryGetInt(this Dictionary dict, string key, out int result)
    {
        result = 0;
        if (dict == null || !dict.ContainsKey(key))
            return false;

        var raw = dict[key];
        return TryConvertToInt(raw, out result);
    }

    public static int GetIntOrDefault(this Dictionary dict, string key, int defaultValue = 0)
    {
        return dict.TryGetInt(key, out var r) ? r : defaultValue;
    }

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
                    ConversionLogger?.Invoke($"DictionaryExtensions.TryConvertToInt failed for value of runtime type: {value?.GetType().Name ?? "null"}");
                    return false;
                }
        }
    }
}

