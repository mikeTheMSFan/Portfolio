#nullable disable
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Models.Content;
using Portfolio.Models.Filters;
using Portfolio.Services.Interfaces;
using Portfolio.ViewModels;

namespace Portfolio.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ICivility _civilityManager;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<BlogUser> _userManager;

    public CommentsController(ApplicationDbContext context,
        UserManager<BlogUser> userManager,
        ICivility civilityManager)
    {
        _context = context;
        _userManager = userManager;
        _civilityManager = civilityManager;
    }

    [TempData(Key = "StatusMessage")] public string StatusMessage { get; set; }


    public async Task<IActionResult> AllComments()
    {
        //get all comments 
        var originalCommentModel = await GetAllCommentsAsync();

        //return comments to the index view.
        return View("Index", originalCommentModel);
    }

    // GET: Comments/Create
    [Authorize]
    public IActionResult Create()
    {
        //get slug from post for routing.
        var slug = GetPostSlug();

        //if the slug is not found, return 404 error.
        if (string.IsNullOrEmpty(slug)) return NotFound();

        //if user tries to access the create method directly, return them from which they came.
        return RedirectToAction("Details", "Posts", new { slug });
    }

    // POST: Comments/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [ValidateReCaptcha]
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] PostViewModel postViewModel, string slug)
    {
        if (ModelState.IsValid == false &&
            ModelState.ContainsKey("Recaptcha") &&
            ModelState["Recaptcha"]!.ValidationState == ModelValidationState.Invalid)
        {
            StatusMessage =
                "Error: reCaptcha error.";
            return RedirectToAction("Details", "Posts", new { slug });
        }
        
        if (ModelState.IsValid)
        {
            if (postViewModel.Comment != null)
            {
                //check if comment has bad words.
                var commentHasFoulLanguage = CheckCommentForBadWords(postViewModel.Comment.Body, slug);

                //if there are bad words, return the user from which they came and let them know.
                if (commentHasFoulLanguage) return RedirectToAction("Details", "Posts", new { slug });

                //set time of comment
                postViewModel.Comment.Created = DateTime.Now.SetKindUtc();

                //add entity to be added in the db.
                _context.Add(postViewModel.Comment);

                //process all actions to the db.
                await _context.SaveChangesAsync();

                //add message to let user know the comment has been added.
                StatusMessage = "Comment added to post.";

                //return user from which they came.
                return RedirectToAction("Details", "Posts", new { slug });
            }

            //there is no comment, therefore, return user from which they came.
            StatusMessage =
                "Error: Something has gone wrong, try again. If this continues please contact administrator.";
            return RedirectToAction("Details", "Posts", new { slug });
        }

        //Model state is not valid, return user from which they came.
        StatusMessage =
            "Error: Something has gone wrong, please note that comments can only be 500 characters in size, try again. If this continues please contact administrator.";
        return RedirectToAction("Details", "Posts", new { slug });
    }

    // GET: Comments/Delete/5
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        //if the comment is not found, return 404 error.
        if (CommentExists(id) == false) return NotFound();

        //get the comment to delete by id.
        var comment = await GetCommentToDeleteByIdAsync(id);

        //return to default view with comment. 
        return View(comment);
    }

    // POST: Comments/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = "Administrator,Moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        //if the comment is not found, return 404 error.
        if (CommentExists(id) == false) return NotFound();

        //get comment to delete.
        var comment = await _context.Comments.FindAsync(id);

        //add entity to be removed from the db.
        _context.Comments.Remove(comment);

        //process all actions to the db.
        await _context.SaveChangesAsync();

        //store data to be returned to the view.
        TempData["StatusMessage"] = "Comment has been deleted";

        //set comment type.
        var commentType = "PermDelete";

        //create comment view model basted on comment type.
        var commentViewModel = await SetCommentsByCommentTypeAsync(commentType);

        //return user to index view with comment view model.
        return View("Index", commentViewModel);
    }

    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> DeletedIndex()
    {
        //get soft delete comments
        var deletedCommentModel = await GetDeletedCommentsAsync();

        //return comments to the index view.
        return View("Index", deletedCommentModel);
    }

    // GET: Comments/Edit/5
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> Edit(Guid id)
    {
        //if no comment is found, return 404 error.
        if (CommentExists(id) == false) return NotFound();

        //get comment by id.
        var comment = await GetCommentByIdAsync(id);

        //store data to be returned to the view.
        ViewData["BlogUserId"] = comment.BlogUser!.Id;

        ViewData["ModeratorId"] = _userManager.GetUserId(User);

        ViewData["PostId"] = comment.Post!.Id;

        //return user to default view with comment.
        return View(comment);
    }

    // POST: Comments/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [Authorize(Roles = "Administrator,Moderator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id,
        [Bind(
            "Id,PostId,BlogUserId,ModeratorId,Body,Created,Updated,Moderated,Deleted,ModeratedBody,ModerationType")]
        Comment comment)
    {
        //if comment is not found, return 404 error.
        if (CommentExists(id) == false) return NotFound();

        if (ModelState.IsValid)
        {
            //get comment to be edited by comment id.
            var newComment = await SetNewCommentByCommentIdAsync(comment.Id);

            //set comment type that that's in temp data to variable.
            var commentType = TempData["commentType"] as string;

            //create comment view model by comment type.
            var commentViewModel = await SetCommentsByCommentTypeAsync(commentType);

            //if there are no comment return 404 error.
            if (newComment == null) return NotFound();

            //get moderator
            var moderator = await _userManager.FindByIdAsync(comment.ModeratorId);

            //if there is no moderator, return 404 error.
            if (moderator == null) return NotFound();

            //set all properties of new comment.
            SetCommentModeration(newComment, moderator);

            //add entity to be updated from the db.
            _context.Update(newComment);

            //process all actions to the db.
            await _context.SaveChangesAsync();

            //store data to be returned to the view.
            TempData["StatusMessage"] = "Comment has been edited successfully.";

            //return user to index view with comment view model.
            return View("Index", commentViewModel);
        }

        //Model state is not valid, return user to edit view.
        TempData["StatusMessage"] =
            "Please check comment and try again, if this continues, please alert the administrator.";
        ViewData["BlogUserId"] = comment.BlogUser!.Id;
        ViewData["ModeratorId"] = _userManager.GetUserId(User);
        ViewData["PostId"] = comment.Post!.Id;
        return View(comment);
    }

    // GET: Comments
    public IActionResult Index()
    {
        //if user tries to access index view directly, return 404 error.
        return NotFound();
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Moderator")]
    public async Task<IActionResult> ModeratedIndex()
    {
        //set delete datetime.
        var deleteDateTime = DateTime.MinValue;

        //set datetime to UTC.
        deleteDateTime = deleteDateTime.SetKindUtc();

        //get moderated comments.
        var moderatedCommentModel = await GetModeratedCommentsAsync();

        //store data to be returned to the view.
        TempData["commentType"] = "Moderated";

        //return user to index view with moderated comments.
        return View("Index", moderatedCommentModel);
    }

    public async Task<IActionResult> SDelete(Guid id)
    {
        //if comment is not found, return 404 error.
        if (CommentExists(id) == false) return NotFound();

        //set comment by id.
        var comment = _context.Comments.FirstOrDefault(c => c.Id == id);

        //get moderator id.
        var moderatorId = _userManager.GetUserId(User);

        //set deleted time
        comment.Deleted = DateTime.Now.SetKindUtc();

        //set moderated time
        comment.Moderated = DateTime.Now.SetKindUtc();

        //set moderator id
        comment.ModeratorId = moderatorId;

        //add entity to be updated from the db.
        _context.Update(comment);

        //process all actions to the db.
        await _context.SaveChangesAsync();

        //set comment type of deleted.
        var commentType = "Deleted";

        //store data to be returned to the view.
        TempData["StatusMessage"] = "Comment has been soft deleted.";

        //set comment view model using comment type.
        var commentViewModel = await SetCommentsByCommentTypeAsync(commentType);

        //return user to index view with soft deleted comments.
        return View("Index", commentViewModel);
    }

    public async Task<IActionResult> SRestore(Guid id)
    {
        //if comment is not found, return 404 error.
        if (CommentExists(id) == false) return NotFound();

        //get comment by id.
        var comment = _context.Comments.FirstOrDefault(c => c.Id == id);

        //get moderator id.
        var moderatorId = _userManager.GetUserId(User);

        //set deleted time. 
        comment.Deleted = DateTime.MinValue.SetKindUtc();

        //set moderated time.
        comment.Moderated = DateTime.Now.SetKindUtc();

        //set moderator id.
        comment.ModeratorId = moderatorId;

        //add entity to be updated from the db.
        _context.Update(comment);

        //process all actions to the db.
        await _context.SaveChangesAsync();

        //set comment type.
        var commentType = "Moderated";

        //set status message.
        TempData["StatusMessage"] = "Comment has been restored.";

        //set comment view model using comment type.
        var commentViewModel = await SetCommentsByCommentTypeAsync(commentType);

        //return user to index view with soft deleted comments.
        return View("Index", commentViewModel);
    }

    private bool CheckCommentForBadWords(string commentBody, string slug)
    {
        var civilityComment = _civilityManager.IsCivil(commentBody);
        if (civilityComment.Virdict == false)
        {
            StatusMessage =
                $"You have {civilityComment.badWords.Count} prohibited words in your comment. Look for: {string.Join(", ", civilityComment.badWords)}.";
            return true;
        }

        return false;
    }

    private bool CommentExists(Guid id)
    {
        return _context.Comments.Any(e => e.Id == id);
    }

    private async Task<CommentViewModel> GetAllCommentsAsync()
    {
        var originalCommentModel = new CommentViewModel
        {
            Comments = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Include(c => c.Post)
                .Where(c => c.Deleted == null && c.Moderated == null).ToListAsync(),
            IsDeleted = false,
            IsModerated = false
        };
        return originalCommentModel;
    }

    private async Task<Comment> GetCommentByIdAsync(Guid id)
    {
        var comment = await _context.Comments
            .Include(c => c.BlogUser)
            .Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == id);

        return comment;
    }

    private async Task<Comment> GetCommentToDeleteByIdAsync(Guid id)
    {
        var comment = await _context.Comments
            .Include(c => c.BlogUser)
            .Include(c => c.Moderator)
            .Include(c => c.Post)
            .FirstOrDefaultAsync(m => m.Id == id);

        return comment;
    }

    private async Task<CommentViewModel> GetDeletedCommentsAsync()
    {
        var deleteDateTime = DateTime.MinValue.Add(TimeSpan.FromMinutes(1));
        deleteDateTime = deleteDateTime.SetKindUtc();
        var deletedCommentModel = new CommentViewModel
        {
            Comments = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Where(c => DateTime.Compare(c.Deleted ?? c.Deleted.Value, deleteDateTime) > 0 &&
                            c.Deleted != null && c.Moderated != null).ToListAsync(),
            IsDeleted = true,
            IsModerated = true
        };

        return deletedCommentModel;
    }

    private async Task<CommentViewModel> GetModeratedCommentsAsync()
    {
        var deleteDateTime = DateTime.MinValue;
        deleteDateTime = deleteDateTime.SetKindUtc();

        var moderatedCommentModel = new CommentViewModel
        {
            Comments = await _context.Comments
                .Include(c => c.Moderator)
                .Where(c => (c.Moderated != null && c.Deleted == null) ||
                            DateTime.Compare(c.Deleted ?? c.Deleted.Value, deleteDateTime) == 0).ToListAsync(),
            IsDeleted = false,
            IsModerated = true
        };

        return moderatedCommentModel;
    }

    private string GetPostSlug()
    {
        var postId = TempData["postId"] as Guid?;

        var slug = _context.Posts.FirstOrDefault(p => p.Id == postId) == null
            ? string.Empty
            : _context.Posts.FirstOrDefault(p => p.Id == postId)?.Slug;

        return slug;
    }

    private Comment SetCommentModeration(Comment comment, BlogUser moderator)
    {
        comment.ModerationType = comment.ModerationType;
        comment.Moderated = DateTime.Now.SetKindUtc();
        comment.Created = comment.Created.SetKindUtc();
        comment.Body =
            $"This comment was flagged by {moderator.FullName} Reason: {comment.ModerationType}";
        comment.ModeratedBody = comment.ModeratedBody;
        comment.ModeratorId = moderator.Id;

        return comment;
    }

    private async Task<CommentViewModel> SetCommentsByCommentTypeAsync(string commentType)
    {
        List<Comment> comments;
        var isModerated = false;
        var isDeleted = false;

        if (commentType.Equals("Moderated"))
        {
            var deleteDateTime = DateTime.MinValue;
            deleteDateTime = deleteDateTime.SetKindUtc();

            comments = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Where(c => (c.Moderated != null && c.Deleted == null) ||
                            DateTime.Compare(c.Deleted ?? c.Deleted.Value, deleteDateTime) == 0)
                .OrderByDescending(c => c.Created).ToListAsync();

            isModerated = true;
        }

        else if (commentType.Equals("Deleted"))
        {
            var deleteDateTime = DateTime.MinValue;
            deleteDateTime = deleteDateTime.SetKindUtc();

            comments = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Where(c => DateTime.Compare(c.Deleted ?? c.Deleted.Value, deleteDateTime) > 0 &&
                            c.Deleted != null && c.Moderated != null)
                .OrderByDescending(c => c.Created).ToListAsync();
            isModerated = true;
            isDeleted = true;
        }

        else
        {
            comments = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Where(c => c.Deleted == null && c.Moderated == null)
                .OrderByDescending(c => c.Created).ToListAsync();
        }

        var commentViewModel = new CommentViewModel
        {
            Comments = comments,
            IsModerated = isModerated,
            IsDeleted = isDeleted
        };

        return commentViewModel;
    }

    private async Task<Comment> SetNewCommentByCommentIdAsync(Guid id)
    {
        var newComment = await _context.Comments
            .Include(c => c.BlogUser)
            .Include(c => c.BlogUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        return newComment;
    }
}