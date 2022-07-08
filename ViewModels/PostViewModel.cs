using Portfolio.Models;
using Portfolio.Models.Content;

namespace Portfolio.ViewModels;

public class PostViewModel
{
    public Post? Post { get; set; } = default!;
    public Comment? Comment { get; set; } = default!;
}