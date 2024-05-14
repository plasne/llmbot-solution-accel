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
}