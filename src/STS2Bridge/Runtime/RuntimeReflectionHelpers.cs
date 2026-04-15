using System.Reflection;

namespace STS2Bridge.Runtime;

internal static class RuntimeReflectionHelpers
{
    private const BindingFlags DefaultFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static bool TryGetString(object? instance, IReadOnlyList<string> memberNames, out string value)
    {
        value = string.Empty;
        if (instance is null)
        {
            return false;
        }

        foreach (var memberName in memberNames)
        {
            var candidate = GetMemberValue(instance, memberName);
            if (candidate is string text && !string.IsNullOrWhiteSpace(text))
            {
                value = text;
                return true;
            }
        }

        return false;
    }

    public static bool TryGetInt(object? instance, IReadOnlyList<string> memberNames, out int value)
    {
        value = default;
        if (instance is null)
        {
            return false;
        }

        foreach (var memberName in memberNames)
        {
            var candidate = GetMemberValue(instance, memberName);
            if (candidate is int number)
            {
                value = number;
                return true;
            }
        }

        return false;
    }

    public static bool TryGetBool(object? instance, IReadOnlyList<string> memberNames, out bool value)
    {
        value = default;
        if (instance is null)
        {
            return false;
        }

        foreach (var memberName in memberNames)
        {
            var candidate = GetMemberValue(instance, memberName);
            if (candidate is bool flag)
            {
                value = flag;
                return true;
            }
        }

        return false;
    }

    public static object? GetMemberValue(object? instance, string memberName)
    {
        if (instance is null)
        {
            return null;
        }

        var type = instance.GetType();
        var property = type.GetProperty(memberName, DefaultFlags);
        if (property is not null)
        {
            return property.GetValue(instance);
        }

        var field = type.GetField(memberName, DefaultFlags);
        if (field is not null)
        {
            return field.GetValue(instance);
        }

        return null;
    }
}
