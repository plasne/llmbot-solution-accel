using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Bot;

public static class Ext
{
    public static void ResetTo(this StringBuilder sb, string value)
    {
        sb.Clear();
        sb.Append(value);
    }

    public static StringContent ToJsonContent<T>(this T obj)
    {
        return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
    }

    public static string ToBase64(this string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static string Truncate(this string value, int length, int maxPayloadSize)
    {
        var trimBy = length - maxPayloadSize + 3;
        return string.Concat(value.AsSpan(0, Math.Max(0, value.Length - trimBy)), "...");
    }
}
