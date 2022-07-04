#nullable disable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Enums;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Services.Interfaces;
using X.PagedList;

namespace Portfolio.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class BlogsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRemoteImageService _remoteImageService;
    private readonly ISlugService _slugService;
    private readonly UserManager<BlogUser> _userManager;
    private readonly IValidate _validate;

    public BlogsController(ApplicationDbContext context, UserManager<BlogUser> userManager,
        ISlugService slugService, IRemoteImageService remoteImageService,
        IValidate validate)
    {
        _context = context;
        _userManager = userManager;
        _slugService = slugService;
        _remoteImageService = remoteImageService;
        _validate = validate;
    }

    [HttpGet]
    [Route("blogs")]
    [Route("blogs/page/{page}")]
    public async Task<IActionResult> AllBlogs(int? page)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 6;

        //gets all blogs as a paged list.
        var blogs = GetAllBlogsAsync(pageNumber, pageSize);

        //Return result to the index view
        return View("Index", await blogs);
    }

    //if users navigate to this route, return not found.
    public IActionResult Articles()
    {
        //if a user tries to access the articles action directly, return not found.
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("blogs")]
    [Route("blogs/search/page/{page}")]
    public async Task<IActionResult> BlogSearch(string term, int? page)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 6;

        //gets blogs based on the term passed in, and returns blogs as a paged list.
        var blogs = await GetBlogByTermAsync(term, pageNumber, pageSize);

        //Return result to the index view
        return View("Index", blogs);
    }

    // GET: Blogs/Create
    //return Create view on get.
    [Authorize(Roles = "Administrator")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Blogs/Create
    // To protect from over-posting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] Blog blog, List<string> categoryValues)
    {
        //checks model state
        if (ModelState.IsValid)
        {
            //use the validate service to make sure the blog passes all checks.
            var result = _validate.BlogEntry(blog, categoryValues);

            //determines if there are model errors, if so, they will be added
            //to model state errors.
            var thereAreModelErrors = CheckForModelErrors(result, categoryValues);

            //if there are model errors, return the errors with the blog to the default view.
            if (thereAreModelErrors) return View(blog);

            //set create date for blog
            blog.Created = DateTime.Now.SetKindUtc(); ;

            //set author id for blog
            blog.AuthorId = _userManager.GetUserId(User);

            //use slug service to turn title into a friendly slug.
            blog.Slug = _slugService.UrlFriendly(blog.Name);

            //take appropriate actions on blog image
            blog = ProcessBlogImage(blog);

            //Add entity to the queue for addition to the db.
            _context.Add(blog);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //Add default category.
            categoryValues.Add("All Posts");

            //Add categories to the db
            AddCategoriesToDatabase(blog.Id, categoryValues);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //if all is well, redirect to the index view.
            return RedirectToAction(nameof(Index));
        }

        //Model state is not valid, return user to create view.
        blog.Image = null;
        ModelState.AddModelError("",
            "There has been an error if this continues, please contact the administrator.");
        ViewData["CategoryValues"] = string.Join(",", categoryValues);
        return View(blog);
    }

    // GET: Blogs/Delete/5
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        //if the blog is not found, return 404 error.
        if (BlogExists(id) == false) return NotFound();

        //get the blog to be deleted by id.
        var blog = await GetBlogByBlogIdAsync(id);

        //return user to the default view with found blog.
        return View(blog);
    }

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    [Route("blogs/DeleteCategory/{name}")]
    public async Task<IActionResult> DeleteCategory(string name)
    {
        //if string is empty let the use know the request was bad.
        if (string.IsNullOrEmpty(name)) return BadRequest();
        
        //get the category based on the passed in name.
        var category = await GetCategoryByNameAsync(name);
        
        //if there is no result, return a 404 error.
        if (category == null) return NotFound();
        
        //if category is all posts, return user to edit page.
        if (category.Name.ToLower() == "all posts")
        {
            return RedirectToAction("Edit", new { id = category.BlogId });
        }

        //return found category to the default view.
        return View(category);
    }

    [HttpPost]
    [ActionName("DeleteCategory")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategoryConfirm(Guid id)
    {
        //if the blog id is not present, return 404 error.
        if (TempData["CurrentBlogId"] == null) return NotFound();

        //assign the applicable blog id to a variable for routing.
        var rootBlogId = (Guid)TempData["CurrentBlogId"];
        
        //if the category is not found, return a 404 error.
        if (CategoryExists(id) == false) return NotFound();

        //get category by passed in id.
        var category = await GetCategoryByIdAsync(id);

        //get posts that need to be transferred to the 'all posts' category.
        var postsToChange = await _context.Posts
            .Where(p => p.Category == category).ToListAsync();

        //move posts to the 'all posts' category.
        MovePostsToDefaultCategory(postsToChange);
        
        //add entity to the list of db actions that need to be executed.
        _context.Remove(category);

        //execute db actions.
        await _context.SaveChangesAsync();

        //return user to the the page from which they came.
        return RedirectToAction("Edit", new { id = rootBlogId });
    }

    // POST: Blogs/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        //if the blog doesn't exist, return a 404 error.
        if (BlogExists(id) == false) return NotFound();
        
        //get the blog to be deleted.
        var blog = await _context.Blogs.FindAsync(id);

        //delete image associated with the blog.
        DeleteBlogImage(blog);

        //add entity to the list of db actions that need to be executed.
        _context.Blogs.Remove(blog!);

        //execute db actions.
        await _context.SaveChangesAsync();

        //return user to the home page.
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    // GET: Blogs/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        //if blog is not found, return a 404 error.
        if (BlogExists(id) == false) return NotFound();

        //get blog with all needed properties by id.
        var blog = await GetBlogWithCategoriesByAuthorIdAsync(id.ToString());

        //return blog to default view.
        return View(blog);
    }

    [HttpGet]
    [Route("blog/{slug}")]
    [Route("blog/{slug}/page/{page}")]
    public async Task<IActionResult> DisplayArticles(string slug, int? page)
    {
        //check for production ready articles.
        var productionReadyArticles = _context.Blogs
            .Include(b => b.Posts)
            .FirstOrDefault(b => b.Slug == slug)!.Posts
            .Where(p => p.ReadyStatus == ReadyStatus.ProductionReady)!
            .Count();

        //if there is no log or production ready articles, return 404 error.
        if (string.IsNullOrEmpty(slug) || productionReadyArticles < 1) return NotFound();

        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 3;

        //get blog with all needed properties by slug.
        var blog = await GetBlogWithCategoriesBySlugAsync(slug);

        //if blog is not found by slug, return 404 error.
        if (blog == null) return NotFound();

        //get all posts by blog id.
        var posts = await GetPostsByBlogIdAsync(blog.Id);

        //get correct paginated posts.
        var postList = await posts.ToPagedListAsync(pageNumber, pageSize);

        //get top ten tags
        var tags = await GetTop10TagsAsync();

        //store data to be returned to the view.
        ViewData["action"] = "DisplayArticles";

        ViewData["tags"] = tags;

        ViewData["posts"] = postList;

        //return the blog to the articles view.
        return View("Articles", blog);
    }

    [HttpGet]
    [Route("blog/{slug}/category/{categoryName}")]
    [Route("blog/{slug}/category/{categoryName}/page/{page}")]
    public async Task<IActionResult> DisplayArticlesByCategory(string slug, int? page, string categoryName)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 3;

        //create a new instance of the blog model.
        var blog = new Blog();

        if (ModelState.IsValid)
        {
            //get blog by slug
            blog = await GetBlogBySlugAsync(slug);

            //if the blog doesn't exist, return a 404 error.
            if (BlogExists(blog.Id) == false) return NotFound();

            //get top 10 tags.
            var tags = await GetTop10TagsAsync();

            //get category by blog id
            var category = GetCategoryByBlogId(blog.Id, categoryName);

            //if the category is not found, return a 404 error.
            if (category == null) return NotFound();

            //get posts by found category.
            var categoryPosts = await GetCategoryPostsAsync(category.Id, categoryName, pageNumber, pageSize);

            //If there are no category posts, assign all blog posts to category posts.
            if (categoryPosts.Count < 1) categoryPosts = CheckForCategoryPosts(blog, categoryPosts, pageNumber, pageSize);

            //store data to be returned to the view.
            ViewData["tags"] = tags;

            ViewData["category"] = categoryName;

            ViewData["action"] = "DisplayArticlesByCategory";

            ViewData["posts"] = categoryPosts;

            //return the blog to the articles view.
            return View("Articles", blog);
        }

        //Model state is not valid, return user to articles view.
        TempData["StatusMessage"] = "The is an error, if this continues, please contact the administrator.";

        return View("Articles", blog);
    }

    // GET: Blogs/Edit/5
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Edit(Guid id)
    {
        //if blog doesn't exist, return 404 error.
        if (BlogExists(id) == false) return NotFound();

        //get blog by id
        var blog = await GetBlogByBlogIdAsync(id);

        //store data to be returned to the view.
        ViewData["CategoryValues"] = string.Join(",", blog.Categories!.Select(c => c.Name));

        ViewData["AuthorId"] = _userManager.GetUserId(User);

        //return blog to default view.
        return View(blog);
    }

    // POST: Blogs/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [FromForm] Blog blog, List<string> categoryValues)
    {
        //if blog is not found, let user know that the request is bad.
        if (BlogExists(id) == false) return BadRequest();

        //create instance of blog that will be updated.
        var blogToUpdate = await _context.Blogs
            .Include(b => b.Posts)
            .Include(b => b.Categories.OrderBy(c => c.Name))
            .FirstOrDefaultAsync(b => b.Id == id);

        if (ModelState.IsValid)
        {
            //set properties of blog to be updated.
            blogToUpdate = SetBlogToUpdate(blog, blogToUpdate);

            //uses the validate service to make sure the blog passes all checks.
            var result = _validate.BlogEntry(blog, categoryValues, blogToUpdate);

            //determines if there are model errors, if so, they will be added
            //to model state errors.
            var thereAreModelErrors = CheckForModelErrors(result, categoryValues);

            //if there are model errors, return the errors with the blog to the default view.
            if (thereAreModelErrors) return View(blog);

            //set blog name.
            blogToUpdate.Name = blog.Name;

            //set slug.
            blogToUpdate.Slug = _slugService.UrlFriendly(blog.Name);

            //update image if necessary.
            if (blogToUpdate.Image != null)
                blogToUpdate = _remoteImageService.UpdateImage(blogToUpdate, blogToUpdate.Image!) as Blog;

            //remove stale categories from blog
            categoryValues = RemoveDuplicateCategories(categoryValues);

            //add categories to db.
            AddCategoriesToDatabase(blogToUpdate!.Id, categoryValues);

            //Add entity to the queue for update to the db.
            _context.Update(blogToUpdate);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //if all is well, redirect to the index view.
            return RedirectToAction(nameof(Index));
        }

        //Model state is not valid, return user to edit view.
        blogToUpdate!.Image = null;
        ModelState.AddModelError("",
            "There has been an error if this continues, please contact the administrator.");
        ViewData["CategoryValues"] = string.Join(",", categoryValues);
        return View(blogToUpdate);
    }

    // GET: Blogs
    [HttpGet]
    public IActionResult Index()
    {
        //if user tries to access the Index action directly, return 404 error.
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchArticles(string term, string authorId, int? page)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 3;

        //if no term is passed in, return bad request error.
        if (term == null) return BadRequest();

        //get a blog with all appropriate properties based on the author id.
        var blog = await GetBlogWithCategoriesByAuthorIdAsync(authorId);

        //if no blog is found, return 404 error.
        if (blog == null) return NotFound();

        //get a list of posts based on the blog id.
        var posts = await GetPostsByBlogIdAsync(blog.Id);

        //get posts based on search term.
        var postList = SearchPostsToPagedListAsync(posts, term, pageNumber, pageSize);

        //get top ten tags.
        var tags = await GetTop10TagsAsync();

        //store data to be returned to the view.
        ViewData["action"] = "DisplayArticles";

        ViewData["tags"] = tags;

        ViewData["posts"] = postList;

        //return the blog to the articles view.
        return View("Articles", blog);
    }

    [HttpGet]
    [Authorize(Roles = "Administrator")]
    [Route("userblogs")]
    [Route("userblogs/page/{page}")]
    public async Task<IActionResult> UserBlogs(int? page)
    {
        //if the page number is null, start on page one.
        var pageNumber = page ?? 1;

        //defines number of results per page.
        var pageSize = 6;

        //gets blogs based on current user
        var blogs = await GetCurrentUserBlogsAsync(pageNumber, pageSize);

        //returns blogs to the Index view.
        return View("Index", blogs);
    }
    
    private void AddCategoriesToDatabase(Guid blogId, List<string> categoryValues)
    {
        foreach (var categoryName in categoryValues)
            _context.Add(new Category
            {
                BlogId = blogId,
                Name = categoryName
            });
    }

    private bool BlogExists(Guid id)
    {
        return _context.Blogs.Any(e => e.Id == id);
    }

    private IPagedList<Post> CheckForCategoryPosts(Blog blog, IPagedList<Post> categoryPosts, int pageNumber,
        int pageSize)
    {
        if (categoryPosts.All(p => p.ReadyStatus != ReadyStatus.ProductionReady))
        {
            return GetNoCategoryBlogPosts(blog, pageNumber, pageSize);
        }

        return new List<Post>().ToPagedList(pageNumber, pageSize);
    }

    private bool CheckForModelErrors(Tuple<List<Tuple<string, string, Blog>>> result, List<string> categoryValues)
    {
        //if there are errors, let the user know.
        if (result.Item1.Any())
        {
            var modelErrors = result.Item1.ToList();
            foreach (var modelError in modelErrors) ModelState.AddModelError(modelError.Item1, modelError.Item2);

            ViewData["CategoryValues"] = string.Join(",", categoryValues);
            return true;
        }

        return false;
    }

    private void DeleteBlogImage(Blog blog)
    {
        if (!string.IsNullOrEmpty(blog.FileName))
        {
            //Set upload directory
            var configuration = GetConfiguration();
            var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
                .FirstOrDefault(d => d.Key == "BlogUploadDirectory")!.Value;

            //delete remote picture
            _remoteImageService.DeleteImage(blog, uploadDirectory);
        }
    }

    private async Task<IPagedList<Blog>> GetAllBlogsAsync(int pageNumber, int pageSize)
    {
        //get a list of all blogs in the system.
        var blogs = await _context.Blogs
            .Include(b => b.Author)
            .Include(b => b.Posts.Where(p => p.ReadyStatus == ReadyStatus.ProductionReady))
            .OrderByDescending(b => b.Created).ToListAsync();

        //create list of blogs that are production ready.
        var blogOutput = new List<Blog>();
        foreach (var blog in blogs)
        {
            if (blog.Posts.Count > 0)
            {
                blogOutput.Add(blog);
            }
        }

        return await blogOutput.ToPagedListAsync(pageNumber, pageSize);;
    }

    private async Task<Blog> GetBlogByBlogIdAsync(Guid id)
    {
        var blog = await _context.Blogs
            .Include(b => b.Categories.OrderBy(c => c.Name))
            .FirstOrDefaultAsync(b => b.Id == id);

        return blog;
    }

    private async Task<Blog> GetBlogBySlugAsync(string slug)
    {
        var blog = await _context.Blogs
            .Include(b => b.Categories.OrderBy(c => c.Name))
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.Slug == slug);

        return blog;
    }

    private async Task<IPagedList<Blog>> GetBlogByTermAsync(string term, int pageNumber, int pageSize)
    {
        var blogs = await _context.Blogs
            .Include(b => b.Author)
            .Where(b => b.Posts.Any(p => p.ReadyStatus == ReadyStatus.ProductionReady) &&
                        b.Name.ToLower().Contains(term.ToLower()))
            .OrderByDescending(b => b.Created)
            .ToPagedListAsync(pageNumber, pageSize);

        return blogs;
    }

    private async Task<Blog> GetBlogWithCategoriesByAuthorIdAsync(string id)
    {
        var blog = await _context.Blogs
            .Include(b => b.Categories.OrderBy(c => c.Name))
            .Include(b => b.Author)
            .FirstOrDefaultAsync(b => b.AuthorId == id);

        return blog;
    }

    private async Task<Blog> GetBlogWithCategoriesBySlugAsync(string slug)
    {
        //get blog based on slug
        var blog = await _context.Blogs
            .Include(b => b.Author)
            .Include(b => b.Categories.OrderBy(c => c.Name))
            .Include(b => b.Posts.Where(p => p.ReadyStatus == ReadyStatus.ProductionReady))
            .FirstOrDefaultAsync(b => b.Slug == slug);

        return blog;
    }

    private Category GetCategoryByBlogId(Guid blogId, string categoryName)
    {
        //get category by blog id and category name
        var category = _context.Categories.FirstOrDefault(c =>
            c.BlogId == blogId && c.Name.ToLower() == categoryName.ToLower());

        return category;
    }

    private async Task<Category> GetCategoryByIdAsync(Guid id)
    {
        //if no category is found, let the user know
        //get category based on id
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);
        return category;
    }

    private async Task<Category> GetCategoryByNameAsync(string name)
    {
        var category = await _context.Categories
            .Include(c => c.Blog)
            .Include(c => c.Blog.Author)
            .FirstOrDefaultAsync(c => c.Name == name);

        return category;
    }

    private async Task<IPagedList<Post>> GetCategoryPostsAsync(Guid id, string categoryName, int pageNumber,
        int pageSize)
    {
        List<Post> categoryPosts;
        //if the category all posts is found get all post for blog
        if (categoryName.ToLower() == "all posts")
            categoryPosts = await _context.Posts
                .Include(p => p.BlogUser)
                .Where(p => p.ReadyStatus == ReadyStatus.ProductionReady)
                .OrderByDescending(p => p.Created).ToListAsync();

        //Get posts by category id
        else
            categoryPosts = await _context.Posts
                .Include(p => p.BlogUser)
                .Where(p => p.CategoryId == id &&
                            p.ReadyStatus == ReadyStatus.ProductionReady)
                .OrderByDescending(p => p.Created).ToListAsync();

        return await categoryPosts.ToPagedListAsync(pageNumber, pageSize);
    }

    private async Task<IPagedList<Blog>> GetCurrentUserBlogsAsync(int pageNumber, int pageSize)
    {
        var blogs = await _context.Blogs
            .Include(b => b.Author)
            .Where(b => b.AuthorId == _userManager.GetUserId(User))
            .OrderByDescending(b => b.Created)
            .ToPagedListAsync(pageNumber, pageSize);

        return blogs;
    }

    private IPagedList<Post> GetNoCategoryBlogPosts(Blog blog, int pageNumber, int pageSize)
    {
        var message =
            "No articles were found for this category, instead here is a list of posts by the user.";

        var output = _context.Posts
            .Where(p => p.BlogId == blog.Id && p.ReadyStatus == ReadyStatus.ProductionReady)
            .ToPagedList(pageNumber, pageSize);

        if (output.Count < 1)
        {
            message =
                "No articles were found for this category.";
        }
        
        ViewData["action"] = "DisplayArticles";
        TempData["StatusMessage"] = message;
        return output;
    }

    private async Task<List<Post>> GetPostsByBlogIdAsync(Guid id)
    {
        var posts = await _context.Posts
            .Include(p => p.Category)
            .Where(p => p.BlogId == id && p.ReadyStatus == ReadyStatus.ProductionReady)
            .OrderByDescending(p => p.Created)
            .ToListAsync();

        return posts;
    }

    private async Task<List<Tag>> GetTop10TagsAsync()
    {
        var tags = await _context.Tags.ToListAsync();
        tags = tags.OrderTagListDescending();
        tags = tags.Take(10).ToList();

        return tags;
    }

    private void MovePostsToDefaultCategory(List<Post> postsToChange)
    {
        if (postsToChange.Count > 0)
            foreach (var post in postsToChange)
            {
                post.CategoryId = _context.Categories.FirstOrDefault(c => c.Name == "All Posts")!.Id;
                _context.Update(post);
            }
    }

    private Blog ProcessBlogImage(Blog blog)
    {
        //check if there is a blog file
        if (blog.Image == null)
        {
            //...nothing to check
        }

        //If there is a blog image, upload it.
        else if (blog.Image != null)
        {
            blog.FileName = _remoteImageService.UploadBlogImage(blog.Image);
        }

        return blog;
    }

    private List<string> RemoveDuplicateCategories(List<string> categories)
    {
        var pulledCategories = _context.Categories.ToList();
        var lowerCaseCategories = categories.ConvertAll(d => d.ToLower());

        foreach (var category in pulledCategories)
        {
            if (lowerCaseCategories.Contains(category.Name.ToLower()))
            {
                lowerCaseCategories.Remove(category.Name.ToLower());
            }
        }

        return lowerCaseCategories;
    }

    private IPagedList<Post> SearchPostsToPagedListAsync(List<Post> posts, string term, int pageNumber,
        int pageSize)
    {
        var pagedPosts = posts.Where(p => p.Title.ToLower().Contains(term.ToLower())
                                          && p.ReadyStatus == ReadyStatus.ProductionReady)
            .ToPagedList(pageNumber, pageSize);

        return pagedPosts;
    }

    private Blog SetBlogToUpdate(Blog blog, Blog blogToUpdate)
    {
        //set blogToUpdate with information provided by user
        blogToUpdate!.Updated = DateTime.Now.SetKindUtc();
        blogToUpdate.Created = blogToUpdate.Created.SetKindUtc();
        blogToUpdate.Name = blog.Name;
        blogToUpdate.Description = blog.Description;
        blogToUpdate.AuthorId = blog.AuthorId;
        blogToUpdate.Image = blog.Image;

        return blogToUpdate;
    }

    private bool CategoryExists(Guid id)
    {
        return _context.Categories.Any(c => c.Id == id);
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
}