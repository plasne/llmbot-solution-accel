using System;
using System.Text;
using Iso8601DurationHelper;

public static class Ext
{
    public static Duration AsDuration(this string value, Func<Duration> dflt)
    {
        if (Duration.TryParse(value, out var duration))
        {
            return duration;
        }
        return dflt();
    }

    public static void ResetTo(this StringBuilder sb, string value)
    {
        sb.Clear();
        sb.Append(value);
    }
}