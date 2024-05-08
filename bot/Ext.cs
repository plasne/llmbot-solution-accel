using System;
using System.Text;

public static class Ext
{
    public static void ResetTo(this StringBuilder sb, string value)
    {
        sb.Clear();
        sb.Append(value);
    }
}