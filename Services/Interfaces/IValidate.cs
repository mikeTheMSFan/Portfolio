using System.Runtime.InteropServices;
using Portfolio.Models;
using Portfolio.Models.Content;

namespace Portfolio.Services.Interfaces;

public interface IValidate
{
    public Tuple<List<Tuple<string, string, Blog>>> BlogEntry(Blog blog, List<string> categoryValues,
        [Optional] Blog? blogToUpdate);

    public Tuple<List<Tuple<string, string, Post>>> PostEntry(Post post, List<string> tagValues, string slug,
        [Optional] Post? newPost);

    public Tuple<List<Tuple<string, string, Project>>> ProjectEntry(Project project);
}