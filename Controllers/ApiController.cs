using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Enums;
using Portfolio.Models;

namespace Portfolio.Controllers;

public class ApiController : Controller
{
    private readonly ApplicationDbContext _context;

    public ApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     Gets the top three posts of a specified blog by date.
    /// </summary>
    /// <returns>5 Blog posts from newest to oldest.</returns>
    /// <response code="404">No posts found for the specified blog</response>
    /// <remarks>
    ///     Sample request:
    ///     GET /api/GetTopThreeBlogPostsByFirstBlog/
    ///     {
    ///     "output": [
    ///     {
    ///     "title": "This is a test post for quickies",
    ///     "abstract": "This is a test post",
    ///     "content": "This is a test post",
    ///     "created": "2022-05-02T18:26:46.128345Z",
    ///     "updated": null,
    ///     "slug": "this-is-a-test-post-for-quickies",
    ///     "blogUser": {
    ///     "firstName": "Michael",
    ///     "lastName": "Robinson",
    ///     "authorDescription": null,
    ///     "facebookUrl": "",
    ///     "instagramUrl": "",
    ///     "pinterestUrl": "",
    ///     "youTubeUrl": "",
    ///     "twitterUrl": ""
    ///     },
    ///     "category": {
    ///     "name": "quickies"
    ///     },
    ///     "tags": [],
    ///     "comments": []
    ///     }
    ///     ]
    ///     }
    /// </remarks>
    [HttpGet]
    [Route("api/GetTopThreeBlogPostsByFirstBlog/")]
    public IActionResult GetTopThreeBlogPostsByBlogId()
    {
        //Set json settings
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };
        
        //get all blogs.
        var blogList = _context.Blogs.ToList();
        
        //get blog id from the oldest blog.
        var firstBlog = blogList.FirstOrDefault(b => b.Created == blogList.Min(blog => blog.Created));
        if (firstBlog == null)
        {
            var noBlogOutput = new List<Blog>();
            return Json(new { noBlogOutput }, options);
        }
        
        var blogId = firstBlog.Id;
        
        //if there is no blog return a new list of post in json form.
        if (BlogExists(blogId) == false)
        {
            var noBlogOutput = new List<Post>();
            return Json(new { noBlogOutput }, options);
        }

        //Get three posts by id
        var output = _context.Posts
            .Include(p => p.BlogUser)
            .Include(p => p.Category)
            .Where(p => p.BlogId == blogId && p.ReadyStatus == ReadyStatus.ProductionReady)
            .OrderByDescending(p => p.Created).Take(3);

        //If there are no post, return a new list of post in json form.
        if (!output.Any())
        {
            var noPostsOutput = new List<Post>();
            return Json(new { noPostsOutput }, options);
        }

        //Hide ids
        foreach (var post in output) post.Category!.Id = default;

        //return posts in json form.
        return Json(new { output }, options);
    }

    /// <summary>
    ///     Gets all posts of a specified blog.
    /// </summary>
    /// <param name="blogId"></param>
    /// <returns>All posts of a specified blog</returns>
    /// <response code="404">No posts found for the specified blog</response>
    /// <remarks>
    ///     Sample request:
    ///     GET /api/getAllPostsBlogsByBlogId/
    ///     {
    ///     "output": [
    ///     {
    ///     "title": "This is a test post for quickies",
    ///     "abstract": "This is a test post",
    ///     "content": "This is a test post",
    ///     "created": "2022-05-02T18:26:46.128345Z",
    ///     "updated": null,
    ///     "slug": "this-is-a-test-post-for-quickies",
    ///     "blogUser": {
    ///     "firstName": "Michael",
    ///     "lastName": "Robinson",
    ///     "authorDescription": null,
    ///     "facebookUrl": "",
    ///     "instagramUrl": "",
    ///     "pinterestUrl": "",
    ///     "youTubeUrl": "",
    ///     "twitterUrl": ""
    ///     },
    ///     "category": {
    ///     "name": "quickies"
    ///     },
    ///     "tags": [],
    ///     "comments": []
    ///     }
    ///     ]
    ///     }
    /// </remarks>
    [HttpGet]
    [Route("api/getAllPostsBlogsByBlogId/{blogId}")]
    public IActionResult GetAllPostsBlogsByBlogId(Guid blogId)
    {
        // get posts by blog id
        var output = _context.Posts
            .Include(p => p.BlogUser)
            .Include(p => p.Category)
            .Where(p => p.BlogId == blogId);

        // if not posts are found let the user know
        if (!output.Any()) return NotFound();

        //hide category id
        foreach (var post in output) post.Category!.Id = default;

        // set json options
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        //return posts in json form.
        return Json(new { output }, options);
    }

    /// <summary>
    ///     searches for post by title based on the supplied term
    /// </summary>
    /// <param name="term"></param>
    /// <returns>All posts that contain the term in the title</returns>
    /// <response code="404">No posts found for the specified term</response>
    /// <remarks>
    ///     Sample request:
    ///     GET /api/searchByPostTitle/
    ///     {
    ///     "output": [
    ///     {
    ///     "title": "This is a test post for quickies",
    ///     "abstract": "This is a test post",
    ///     "content": "This is a test post",
    ///     "created": "2022-05-02T18:26:46.128345Z",
    ///     "updated": null,
    ///     "slug": "this-is-a-test-post-for-quickies",
    ///     "blogUser": {
    ///     "firstName": "Michael",
    ///     "lastName": "Robinson",
    ///     "authorDescription": null,
    ///     "facebookUrl": "",
    ///     "instagramUrl": "",
    ///     "pinterestUrl": "",
    ///     "youTubeUrl": "",
    ///     "twitterUrl": ""
    ///     },
    ///     "category": {
    ///     "name": "quickies"
    ///     },
    ///     "tags": [],
    ///     "comments": []
    ///     }
    ///     ]
    ///     }
    /// </remarks>
    [HttpGet]
    [Route("api/searchByPostTitle/{term}")]
    public IActionResult SearchByPostTitle(string term)
    {
        //get posts by user defined terms
        var output = _context.Posts
            .Include(p => p.BlogUser)
            .Include(p => p.Category)
            .Where(p => p.Title.ToLower().Contains(term.ToLower()));

        //if no posts are found, let user know
        if (!output.Any()) return NotFound();

        //hide category id
        foreach (var post in output) post.Category!.Id = default;

        // set json options
        JsonSerializerOptions options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        //return posts in json form.
        return Json(new { output }, options);
    }

    private bool BlogExists(Guid id)
    {
        return _context.Blogs.Any(e => e.Id == id);
    }
}