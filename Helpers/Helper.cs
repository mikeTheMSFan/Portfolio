using System.Text.RegularExpressions;

namespace Portfolio.Helpers;

public static class Helper
{
    public static bool ValidHttpUrl(string s, out Uri? resultUri)
    {
        if (!Regex.IsMatch(s, @"^https?:\/\/", RegexOptions.IgnoreCase))
        {
            resultUri = null;
            return false;
        }

        if (Uri.TryCreate(s, UriKind.Absolute, out resultUri))
            return resultUri.Scheme == Uri.UriSchemeHttp ||
                   resultUri.Scheme == Uri.UriSchemeHttps;

        return false;
    }
}