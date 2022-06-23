using Portfolio.Data;
using Portfolio.Extensions;
using Portfolio.Services.Interfaces;

namespace Portfolio.Services;

public class BasicSlugService : ISlugService
{
    private readonly ApplicationDbContext _context;

    public BasicSlugService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string UrlFriendly(string title)
    {
        //return url friendly slug.
        return title.Slugify();
    }

    public bool IsUnique(string slug)
    {
        //check if a post exists with the passed in slug. 
        return !_context.Posts.Any(p => p.Slug!.ToLower() == slug.ToLower());
    }
}