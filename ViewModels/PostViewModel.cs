using Portfolio.Models;

namespace Portfolio.ViewModels;

public class PostViewModel
{
    public Post? Post { get; set; } = default!;
    public Comment? Comment { get; set; } = default!;
}