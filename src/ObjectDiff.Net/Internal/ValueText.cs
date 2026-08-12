using System.Globalization;

namespace ObjectDiff.Internal;

internal static class ValueText
{
    private const string NullText = "null";

    public static string Raw(object? value)
    {
        switch (value)
        {
            case null:
                return NullText;
            case string text:
                return text;
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            default:
                return value.ToString() ?? NullText;
        }
    }

    public static string Quoted(object? value) =>
        value is string text ? "\"" + text + "\"" : Raw(value);
}
