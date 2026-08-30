using System.Globalization;
using System.Text;

namespace Departures;

public static class TextSearch
{
    public static string Fold(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        string formD = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (char c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool Matches(string? haystack, string needleFolded)
    {
        if (needleFolded.Length == 0)
            return true;

        return Fold(haystack).Contains(needleFolded, StringComparison.Ordinal);
    }
}
