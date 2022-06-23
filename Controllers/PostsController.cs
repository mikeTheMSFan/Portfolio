#nullable disable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Enums;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Services.Interfaces;
using Portfolio.ViewModels;
using X.PagedList;

namespace Portfolio.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class PostsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRemoteImageService _remoteImageService;
    private readonly ISlugService _slugService;
    private readonly UserManager<BlogUser> _userManager;
    private readonly IValidate _validate;

    public PostsController(ApplicationDbContext context,
        ISlugService slugService,
        UserManager<BlogUser> userManager,
        IRemoteImageService remoteImageService,
        IValidate validate)
    {
        _context = context;
        _slugService = slugService;
        _userManager = userManager;
        _remoteImageService = remoteImageService;
        _validate = validate;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    [Route("userposts")]
    [Route("userposts/page/{page}")]
    public async Task<IActionResult> AllAuthorPosts(int? page)
    {
        //get blog user id based on current user.
        var blogUserId = _userManager.GetUserId(User);

        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 10;

        //get posts based on blog user id.
        var posts = GetPostsByAuthorId(blogUserId);

        //return posts to the author index view.
        return View("AuthorIndex", await posts.ToPagedListAsync(pageNumber, pageSize));
    }

    public IActionResult AuthorIndex()
    {
        return NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AuthorSearch(string term, int? page)
    {
        //get blog user id based on current user.
        var blogUserId = _userManager.GetUserId(User);

        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 10;

        //get posts based on blog user id.
        var posts = GetPostsByAuthorId(blogUserId);

        //return posts to the author index view.
        return View("AuthorIndex", await posts.ToPagedListAsync(pageNumber, pageSize));
    }

    // GET: Posts/Create
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public IActionResult Create()
    {
        //get blog user id based on current user.
        var blogUserId = _userManager.GetUserId(User);

        //get blog based on user id.
        var blog = _context.Blogs.FirstOrDefault(b => b.AuthorId == blogUserId);

        //if blog is not found, return 404 error.
        if (blog == null) return NotFound();

        //define the blog id.
        var blogId = blog.Id;

        //store data to be returned to the view.
        ViewData["BlogId"] = new SelectList(_context.Blogs.Where(b => b.AuthorId == blogUserId), "Id", "Name");

        ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.BlogId == blogId), "Id", "Name");

        //return the default view.
        return View();
    }

    // POST: Posts/Create
    // To protect from over-posting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] Post post, List<string> tagValues)
    {
        //Get all model state errors
        if (ModelState.IsValid)
        {
            //turn title into friendly slug.
            var slug = _slugService.UrlFriendly(post.Title);

            //uses the validate service to make sure the post passes all checks.
            var result = _validate.PostEntry(post, tagValues, slug);

            //determines if there are model errors, if so, they will be added
            //to model state errors.
            var thereAreModelErrors = CheckForModelErrors(result, tagValues);

            //if there are model errors, return the errors with the blog to the default view.
            if (thereAreModelErrors) return View(post);

            //assign generated slug to post model.
            post.Slug = slug;

            //assign date to post model.
            post.Created = DateTime.Now.SetKindUtc();

            //take appropriate actions on post image
            post = ProcessPostImage(post);

            //Add entity to the queue for addition to the db.
            _context.Add(post);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //add tags to the db.
            AddTagsToDatabase(post, tagValues);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //if all is well, redirect to the index view.
            return RedirectToAction(nameof(Index));
        }

        //Model state is not valid, return user to create view.
        ViewData["TagValues"] = string.Join(",", tagValues);
        var blogUserIdM = _userManager.GetUserId(User);
        ViewData["BlogUserId"] = blogUserIdM;
        ViewData["BlogId"] = new SelectList(_context.Blogs.Where(b => b.AuthorId == blogUserIdM), "Id", "Name");
        return View(post);
    }

    // GET: Posts/Delete/5
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        //if the post is not found, return 404 error.
        if (PostExists(id) == false) return NotFound();

        //get the post to be deleted by id.
        var post = await GetPostByIdAsync(id);

        ////return user to the default view with found post.
        return View(post);
    }

    // POST: Posts/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        //if the post is not found, return 404 error.
        if (PostExists(id) == false) return NotFound();

        //get the post to be deleted.
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id);

        //delete image associated with the post.
        DeletePostImage(post);

        //add entity to the list of db actions that need to be executed.
        _context.Posts.Remove(post);

        //execute db actions.
        await _context.SaveChangesAsync();

        //return user to the home page.
        return RedirectToAction(nameof(Index));
    }

    // GET: Posts/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(string slug)
    {
        //if no slug id passed in, return bad request.
        if (string.IsNullOrEmpty(slug)) return BadRequest();

        //get post using the passed in slug
        var post = await GetPostBySlugAsync(slug);

        //if there is no post or blog associated with the post,
        //return not found.
        if (post == null || post.Blog == null) return NotFound();

        //get top ten tags.
        var tags = await GetTop10TagsAsync();

        //assign post to the post view model.
        var model = new PostViewModel { Post = post };

        //store data to be returned to the view.
        ViewData["tags"] = tags;

        //return model to default view.
        return View(model);
    }

    // GET: Posts/Edit/5
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Edit(Guid id)
    {
        //if no post is found, return 404 error.
        if (PostExists(id) == false) return NotFound();

        //create an instance of the post to be edited.
        var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id);

        //store data to be returned to the view.
        ViewData["Categories"] =
            new SelectList(_context.Categories.OrderBy(c => c.Name), "Id", "Name", post.Category);

        ViewData["TagValues"] = string.Join(",", post.Tags!.Select(t => t.Text));

        //return post to the default view.
        return View(post);
    }

    // POST: Posts/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [FromForm] Post post, List<string> tagValues)
    {
        //if no post is found, return 404 error.
        if (PostExists(id) == false) return NotFound();

        if (ModelState.IsValid)
        {
            //get post to be edited.
            var newPost = await _context.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == post.Id);

            //set post properties.
            newPost = SetPostToUpdate(post, newPost);

            //create new slug based on new post title.
            var newSlug = _slugService.UrlFriendly(newPost.Title);

            //uses the validate service to make sure the blog passes all checks.
            var result = _validate.PostEntry(post, tagValues, newSlug, newPost);

            //determines if there are model errors, if so, they will be added
            //to model state errors.
            var thereAreModelErrors = CheckForModelErrors(result, tagValues);

            //if there are model errors, return the errors with the blog to the default view.
            if (thereAreModelErrors) return View(post);

            //remove stale tags
            RemoveStaleTags(newPost);

            //update image if necessary.
            if (newPost.Image != null) newPost = _remoteImageService.UpdateImage(newPost, newPost.Image!) as Post;

            //add tags to db.
            AddTagsToDatabase(newPost, tagValues);

            //Add entity to the queue for update to the db.
            _context.Update(newPost!);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //if all is well, redirect to the index view.
            return RedirectToAction(nameof(Index));
        }

        //Model state is not valid, return user to edit view.
        post.Image = null;
        ViewData["TagValues"] = string.Join(',', tagValues);
        return View(post);
    }

    //Get categories based on blog id.
    [HttpGet]
    public async Task<IActionResult> GetCategories(Guid id)
    {
        //get categories based on blog id.
        var categories = await _context.Categories
            .Where(c => c.BlogId == id)
            .OrderBy(c => c.Name).ToListAsync();

        //return categories in json form.
        return Json(new { categoryList = categories });
    }

    // GET: Posts
    [HttpGet]
    [Route("articles")]
    [Route("articles/{page}")]
    public async Task<IActionResult> Index(int? page)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 6;

        //get all posts on the site.
        var posts = _context.Posts.Include(p => p.Blog)
            .Include(p => p.BlogUser)
            .Where(p => p.ReadyStatus == ReadyStatus.ProductionReady);

        //get four most recent post for widget.
        var articles = await _context.Posts.OrderByDescending(p => p.Created).Take(4).ToListAsync();

        //store data to be returned to the view.
        ViewData["articles"] = articles;

        //return posts to default view.
        return View(await posts.ToPagedListAsync(pageNumber, pageSize));
    }

    [HttpGet]
    [Route("tagCloud/{tagValue}")]
    [Route("tagCloud/{tagValue}/page/{page}")]
    public async Task<IActionResult> Tag(int? page, string tagValue)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 3;

        //get posts based on the passed in tag value.
        var posts = GetPostsByTag(tagValue, pageNumber, pageSize);

        //get top 10 tags
        var tags = await GetTop10TagsAsync();

        //store data to be returned to the view.
        ViewData["TagValue"] = tagValue;

        ViewData["RecentArticles"] = _context.Posts.OrderByDescending(p => p.Created).ToList();

        ViewData["tags"] = tags;

        //return posts to default view.
        return View(posts);
    }

    private void AddTagsToDatabase(Post post, List<string> tagValues)
    {
        foreach (var tag in tagValues)
            _context.Add(new Tag
            {
                PostId = post.Id,
                BlogUserId = post.BlogUserId,
                Text = tag
            });
    }

    private bool CheckForModelErrors(Tuple<List<Tuple<string, string, Post>>> result, List<string> tagValues)
    {
        //If there are errors, let the user know.
        if (result.Item1.Any())
        {
            var modelErrors = result.Item1.ToList();
            var blogUserId = _userManager.GetUserId(User);
            var blog = _context.Blogs.FirstOrDefault(b => b.AuthorId == blogUserId);
            foreach (var modelError in modelErrors) ModelState.AddModelError(modelError.Item1, modelError.Item2);
            ViewData["BlogId"] = new SelectList(_context.Blogs.Where(b => b.AuthorId == blogUserId), "Id", "Name");
            ViewData["CategoryId"] = new SelectList(_context.Categories.Where(c => c.BlogId == blog.Id), "Id", "Name");
            ViewData["TagValues"] = string.Join(",", tagValues);
            return true;
        }

        return false;
    }

    private void DeletePostImage(Post post)
    {
        if (!string.IsNullOrEmpty(post.FileName))
        {
            //Set upload directory
            var configuration = GetConfiguration();
            var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
                .FirstOrDefault(d => d.Key == "PostUploadDirectory")!.Value;

            //delete remote picture
            _remoteImageService.DeleteImage(post, uploadDirectory);
        }
    }

    private async Task<Post> GetPostByIdAsync(Guid id)
    {
        var post = await _context.Posts
            .Include(p => p.Blog)
            .Include(p => p.BlogUser)
            .FirstOrDefaultAsync(m => m.Id == id);

        return post;
    }

    private async Task<Post> GetPostBySlugAsync(string slug)
    {
        var post = await _context.Posts
            .Include(p => p.Blog)
            .Include(p => p.BlogUser)
            .Include(p => p.Tags)
            .Include(p => p.Comments.Where(c => c.Deleted == null))
            .FirstOrDefaultAsync(m => m.Slug == slug);

        post!.Blog!.Posts = await _context.Posts.Where(p => p.BlogId == post.Blog.Id)
            .OrderByDescending(p => p.Created).ToListAsync();

        post.Blog.Categories = await _context.Categories.Where(c => c.BlogId == post.Blog.Id).OrderBy(c => c.Name)
            .ToListAsync();

        return post;
    }

    private IQueryable<Post> GetPostsByAuthorId(string id)
    {
        var posts = _context.Posts
            .Include(p => p.Blog)
            .Include(p => p.BlogUser)
            .Include(p => p.Category)
            .Where(p => p.BlogUserId == id);

        return posts;
    }

    private IPagedList<Post> GetPostsByTag(string tagValue, int pageNumber, int pageSize)
    {
        var posts = _context.Posts
            .Include(p => p.BlogUser)
            .Include(p => p.Tags)
            .Include(p => p.Category)
            .Where(p => p.Tags.Any(t => t.Text.ToLower() == tagValue.ToLower())).ToPagedList(pageNumber, pageSize);

        return posts;
    }

    private async Task<List<Tag>> GetTop10TagsAsync()
    {
        var tags = await _context.Tags.ToListAsync();
        tags = tags.OrderTagListDescending();
        tags = tags.Take(10).ToList();

        return tags;
    }

    private Post ProcessPostImage(Post post)
    {
        if (post.Image != null) post.FileName = _remoteImageService.UploadPostImage(post.Image);

        return post;
    }

    private void RemoveStaleTags(Post newPost)
    {
        //Check model for errors...
        if (newPost.Tags != null) _context.Tags.RemoveRange(newPost.Tags);
    }

    private Post SetPostToUpdate(Post post, Post newPost)
    {
        newPost!.Created = newPost!.Created.SetKindUtc();
        newPost!.Updated = DateTime.Now.SetKindUtc();
        newPost!.Abstract = post.Abstract;
        newPost!.Title = post.Title;
        newPost!.ReadyStatus = post.ReadyStatus;
        newPost!.Content = post.Content;
        newPost!.Image = post.Image;
        newPost!.CategoryId = post.CategoryId;

        return newPost;
    }

    private IConfigurationRoot GetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()
            .Decrypt("CipherKey", "CipherText:");

        return configuration;
    }

    private bool PostExists(Guid id)
    {
        return _context.Posts.Any(e => e.Id == id);
    }
}