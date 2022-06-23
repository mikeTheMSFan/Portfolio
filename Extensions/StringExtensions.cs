using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Portfolio.Extensions;

//Marothi Mahlake - https://www.c-sharpcorner.com/blogs/make-url-slugs-in-asp-net-using-c-sharp2
public static class StringExtensions
{
    /// <summary>
    ///     Removes all accents from the input string.
    /// </summary>
    /// <param name="text">The input string.</param>
    /// <returns></returns>
    private static string RemoveAccents(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        text = text.Normalize(NormalizationForm.FormD);
        var chars = text
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c)
                        != UnicodeCategory.NonSpacingMark).ToArray();

        return new string(chars).Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    ///     Turn a string into a slug by removing all accents,
    ///     special characters, additional spaces, substituting
    ///     spaces with hyphens and making it lower-case.
    /// </summary>
    /// <param name="phrase">The string to turn into a slug.</param>
    /// <returns></returns>
    public static string Slugify(this string phrase)
    {
        // Remove all accents and make the string lower case.  
        var output = phrase.RemoveAccents().ToLower();

        // Remove all special characters from the string.  
        output = Regex.Replace(output, @"[^A-Za-z0-9\s-]", "");

        // Remove all additional spaces in favour of just one.  
        output = Regex.Replace(output, @"\s+", " ").Trim();

        // Replace all spaces with the hyphen.  
        output = Regex.Replace(output, @"\s", "-");

        // Return the slug.  
        return output;
    }

    public static string RemoveHtmlTags(this string html)
    {
        var rx = new Regex("<[^>]*>");

        var noHtmlString = rx.Replace(html, "");

        return noHtmlString;
    }
}