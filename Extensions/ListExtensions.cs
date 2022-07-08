using Portfolio.Models;
using Portfolio.Models.Filters;

namespace Portfolio.Extensions;

public static class ListExtensions
{
    public static List<Tag> OrderTagListDescending(this List<Tag> tags)
    {
        var compareDictionary = new Dictionary<Tag, int>();
        foreach (var tag in tags)
            if (compareDictionary.Keys.ToList().All(t => t.Text.ToLower() != tag.Text.ToLower()))
            {
                compareDictionary.Add(tag, 1);
            }
            else
            {
                if (compareDictionary.Keys.Any(t => t.Text == tag.Text))
                {
                    var foundTag = compareDictionary.FirstOrDefault(c => c.Key.Text == tag.Text);
                    var value = foundTag.Value;
                    value += 1;
                    compareDictionary.Add(tag, value);
                    compareDictionary.Remove(foundTag.Key);
                }
            }

        compareDictionary = compareDictionary.OrderByDescending(c => c.Value).ToDictionary(x => x.Key, x => x.Value);
        tags.Clear();
        foreach (var entry in compareDictionary) tags.Add(entry.Key);

        return tags;
    }
}