using System.Text;
using System.Text.RegularExpressions;

namespace Homeboard.Boards.Services;

public static partial class SlugNormalizer
{
    public static string Normalize(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        var ascii = StripDiacritics(lower);
        var hyphenated = NonSlugChars().Replace(ascii, "-");
        return hyphenated.Trim('-');
    }

    private static string StripDiacritics(string s)
    {
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();
}
