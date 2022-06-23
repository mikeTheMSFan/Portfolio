using System.Runtime.InteropServices;
using Portfolio.Data;
using Portfolio.Extensions;
using Portfolio.Helpers;
using Portfolio.Models;
using Portfolio.Services.Interfaces;

namespace Portfolio.Services;

public class ValidateService : IValidate
{
    private readonly ICivility _civilityService;
    private readonly ApplicationDbContext _context;
    private readonly ISlugService _slugService;

    public ValidateService(ApplicationDbContext context,
        ICivility civilityService,
        ISlugService slugService)
    {
        _context = context;
        _civilityService = civilityService;
        _slugService = slugService;
        BlogModelStateErrors = new List<Tuple<string, string, Blog>>();
        PostModelStateErrors = new List<Tuple<string, string, Post>>();
        ProjectModelStateErrors = new List<Tuple<string, string, Project>>();
    }

    private List<Tuple<string, string, Blog>> BlogModelStateErrors { get; }
    private List<Tuple<string, string, Post>> PostModelStateErrors { get; }
    private List<Tuple<string, string, Project>> ProjectModelStateErrors { get; }
    public bool ValidationError { get; set; }

    public Tuple<List<Tuple<string, string, Blog>>> BlogEntry(Blog blog, List<string> categoryValues,
        [Optional] Blog? blogToUpdate)
    {
        //run checks
        CheckBlogNameForDuplicate(blog, blogToUpdate);
        CheckBlogNameForBadWords(blog);
        CheckDescriptionForBadWords(blog);
        CheckCategoriesForBadWords(blog, categoryValues);
        CheckImageForValidity(blog);
        CheckSlugForDuplicate(blog, blogToUpdate);

        //if there are validation errors, let the user know.
        if (ValidationError)
        {
            blog.Image = null;
            return new Tuple<List<Tuple<string, string, Blog>>>(BlogModelStateErrors);
        }

        //if there are no validation errors, return an empty tuple.
        return new Tuple<List<Tuple<string, string, Blog>>>(new List<Tuple<string, string, Blog>>());
    }

    public Tuple<List<Tuple<string, string, Post>>> PostEntry(Post post, List<string> tagValues, string slug,
        [Optional] Post? newPost)
    {
        //run checks
        CheckPostSlugForErrors(post, newPost, slug);
        CheckPostImageForErrors(post);
        CheckPostForBadWords(post);
        CheckTagsForBadWords(post, tagValues);
        CheckTagsForDuplicates(post, tagValues);
        CheckPostSlugForDuplicate(post, newPost);

        //if there are validation errors, let the user know.
        if (ValidationError)
        {
            post.Image = null;
            return new Tuple<List<Tuple<string, string, Post>>>(PostModelStateErrors);
        }

        //if there are no validation errors, return an empty tuple.
        return new Tuple<List<Tuple<string, string, Post>>>(new List<Tuple<string, string, Post>>());
    }

    public Tuple<List<Tuple<string, string, Project>>> ProjectEntry(Project project)
    {
        //run checks
        CheckProjectImage(project);
        CheckProjectFieldForBadWords(project);
        CheckProjectUrl(project);

        //if there are validation errors, let the user know.
        if (ValidationError)
        {
            project.Image = null;
            return new Tuple<List<Tuple<string, string, Project>>>(ProjectModelStateErrors);
        }

        //if there are no validation errors, return an empty tuple.
        return new Tuple<List<Tuple<string, string, Project>>>(new List<Tuple<string, string, Project>>());
    }

    private void CheckProjectImage(Project project)
    {
        if (project.Image == null)
        {
            //...nothing to check.
        }

        else if ((project.Image != null && project.Image.IsImage() == false)
                 || project.Image!.IsDecent() == false)
        {
            ProjectModelStateErrors.Add(new Tuple<string, string, Project>("", "Please check your image.", project));
            ProjectModelStateErrors.Add(
                new Tuple<string, string, Project>("Image", "Please check your image and try again.", project));
            ValidationError = true;
        }
    }

    private void CheckProjectUrl(Project project)
    {
        var isUrl = Helper.ValidHttpUrl(project.ProjectUrl, out _);

        if (isUrl == false)
        {
            ProjectModelStateErrors.Add(new Tuple<string, string, Project>("", "Please check project URL.", project));
            ProjectModelStateErrors.Add(new Tuple<string, string, Project>("ProjectUrl",
                "Please make sure that your link is in the form of a URL.", project));
            ValidationError = true;
        }
    }

    private void CheckProjectFieldForBadWords(Project project)
    {
        var civilityProjectType = _civilityService.IsCivil(project.Type);
        var civilityProjectTitle = _civilityService.IsCivil(project.Title);
        var civilityDescription = _civilityService.IsCivil(project.Description);

        if (civilityProjectType.Virdict == false ||
            civilityProjectTitle.Virdict == false ||
            civilityDescription.Virdict == false)
        {
            var badWordCount = civilityProjectType.badWords.Count + civilityProjectTitle.badWords.Count +
                               civilityDescription.badWords.Count;
            var allBadWords =
                $"{string.Join(" ", civilityProjectType.badWords)} {string.Join(" ", civilityProjectTitle.badWords)} {string.Join(" ", civilityDescription.badWords)}";

            ProjectModelStateErrors.Add(new Tuple<string, string, Project>("",
                $"Please check your work, we've found {badWordCount} bad words. Look for: {allBadWords}", project));

            ValidationError = true;
        }
    }

    private void CheckPostSlugForErrors(Post post, Post? newPost, string slug)
    {
        //if newPost is null, it is a new post.
        if (newPost == null)
        {
            if (string.IsNullOrEmpty(slug))
            {
                PostModelStateErrors.Add(new Tuple<string, string, Post>("Title",
                    "The title you've provided cannot be used as it results in a empty slug.", post));

                ValidationError = true;
            }

            //If the slug isn't unique, return error.
            else if (!_slugService.IsUnique(slug))
            {
                PostModelStateErrors.Add(new Tuple<string, string, Post>("", "Please check the title.", post));
                PostModelStateErrors.Add(new Tuple<string, string, Post>("Title",
                    "The title you've provided cannot be used as it results in a duplicate slug.", post));

                ValidationError = true;
            }
        }
    }

    private void CheckPostSlugForDuplicate(Post post, Post? newPost)
    {
        //if this is not null, it is an post edit
        if (newPost != null)
        {
            var newSlug = _slugService.UrlFriendly(newPost.Title);
            //if the new slug isnt the inital slug
            if (newSlug != newPost.Slug)
            {
                if (_slugService.IsUnique(newSlug))
                {
                    newPost.Title = post.Title;
                    newPost.Slug = newSlug;
                }
                else
                {
                    PostModelStateErrors.Add(new Tuple<string, string, Post>("Title",
                        "This title cannot be used as it results in a duplicate slug", newPost));

                    ValidationError = true;
                }
            }
        }
    }

    private void CheckPostImageForErrors(Post post)
    {
        if (post.Image == null)
        {
            //...nothing to check.
        }

        else if ((post.Image != null && post.Image.IsImage() == false)
                 || post.Image!.ImageSizeIsCorrect() == false
                 || post.Image!.IsDecent() == false)
        {
            PostModelStateErrors.Add(new Tuple<string, string, Post>("", "Please check your image.", post));
            PostModelStateErrors.Add(new Tuple<string, string, Post>("Image",
                "Please check your image and try again.", post));

            ValidationError = true;
        }
    }

    private void CheckPostForBadWords(Post post)
    {
        var noHtmlPostContent = post.Content.RemoveHtmlTags();
        var civilityPostTitle = _civilityService.IsCivil(post.Title);
        var civilityAbstract = _civilityService.IsCivil(post.Abstract!);
        var civilityContent = _civilityService.IsCivil(noHtmlPostContent);

        if (civilityPostTitle.Virdict == false ||
            civilityAbstract.Virdict == false ||
            civilityContent.Virdict == false)
        {
            PostModelStateErrors.Add(new Tuple<string, string, Post>("",
                $"You have {civilityPostTitle.badWords.Count} prohibited words in your title, {civilityAbstract.badWords.Count} prohibited words in your abstract, and {civilityContent.badWords.Count} in your content. Please looks for {string.Join(", ", civilityPostTitle.badWords)} {string.Join(", ", civilityAbstract.badWords)} {string.Join(",", civilityContent.badWords)}",
                post));

            ValidationError = true;
        }
    }

    private void CheckTagsForBadWords(Post post, List<string> tagValues)
    {
        var tagBadWords = new List<string>();
        foreach (var tagText in tagValues)
        {
            var civilityCheck = _civilityService.IsCivil(tagText);
            if (civilityCheck.Virdict == false) tagBadWords.Add(civilityCheck.badWords.FirstOrDefault()!);
        }

        if (tagBadWords.Count is > 0 and <= 5)
        {
            foreach (var badWord in tagBadWords)
            {
                PostModelStateErrors.Add(new Tuple<string, string, Post>("",
                    $"The word {badWord} is prohibited, please choose another", post));
                ValidationError = true;
            }
        }

        else if (tagBadWords.Count is > 0 and > 5)
        {
            PostModelStateErrors.Add(new Tuple<string, string, Post>("",
                "You have more than 5 prohibited words in your title or description, please revise.", post));

            ValidationError = true;
        }
    }

    private void CheckTagsForDuplicates(Post post, List<string> tagValues)
    {
        tagValues = tagValues.ConvertAll(t => t.ToLower());
        var duplicates = tagValues.GroupBy(t => t).Where(t => t.Count() > 1).Select(t => t.Key);

        var enumerable = duplicates.ToList();
        if (enumerable.Any())
        {
            PostModelStateErrors.Add(new Tuple<string, string, Post>("",
                $"Your tags have duplicates, please check your list for: {string.Join(",", enumerable)}", post));
            ValidationError = true;
        }
    }

    private void CheckBlogNameForDuplicate(Blog blog, [Optional] Blog? blogToUpdate)
    {
        //if this is null, it is a new blog.
        if (blogToUpdate == null)
        {
            //Check if blog name is in the DB.
            var nameIsUnique = !_context.Blogs.Any(b => b.Name.ToLower() == blog.Name.ToLower());

            //if the blog name is in the DB, let the user know.
            if (nameIsUnique == false)
            {
                BlogModelStateErrors.Add(new Tuple<string, string, Blog>("", "Please check the blog name.", blog));
                BlogModelStateErrors.Add(new Tuple<string, string, Blog>("Name",
                    "The name you've provided cannot be used as there is already a blog using it.", blog));

                ValidationError = true;
            }
        }
    }

    private void CheckBlogNameForBadWords(Blog blog)
    {
        //Check for bad words.
        var civilityBlogName = _civilityService.IsCivil(blog.Name);

        //If there are bad words, let the user know.
        if (civilityBlogName.Virdict == false)
        {
            var blogNameBadWords = string.Join(", ", civilityBlogName.badWords);

            BlogModelStateErrors.Add(new Tuple<string, string, Blog>("",
                $"You have {civilityBlogName.badWords.Count} prohibited words in your title. Look for: {blogNameBadWords}.",
                blog));

            ValidationError = true;
        }
    }

    private void CheckDescriptionForBadWords(Blog blog)
    {
        var civilityBlogDescription = _civilityService.IsCivil(blog.Description);

        if (civilityBlogDescription.Virdict == false)
        {
            var blogDescriptionBadWords = string.Join(", ", civilityBlogDescription.badWords);

            BlogModelStateErrors.Add(new Tuple<string, string, Blog>("",
                $"You have {civilityBlogDescription.badWords.Count} prohibited words in your description. Look for: {blogDescriptionBadWords}",
                blog));

            ValidationError = true;
        }
    }

    private void CheckCategoriesForBadWords(Blog blog, List<string> categoryValues)
    {
        //Check all provided categories for bad words.
        var categoryBadWords = new List<string>();
        foreach (var categoryText in categoryValues)
        {
            var civilityCheck = _civilityService.IsCivil(categoryText);
            if (civilityCheck.Virdict == false) categoryBadWords.Add(civilityCheck.badWords.FirstOrDefault()!);

            //Let the user know if a they used a word that is not allowed.
            if (categoryText.ToLower() == "unassigned" ||
                categoryText.Contains("unassigned"))
            {
                BlogModelStateErrors.Add(new Tuple<string, string, Blog>("",
                    $"The word \"{categoryText}\" cannot be used, please choose another category.", blog));

                ValidationError = true;
            }
        }

        //Let the user know if 1 to 5 bad words are in their categories.
        if (categoryBadWords.Count is > 0 and <= 5)
        {
            foreach (var badWord in categoryBadWords)
                BlogModelStateErrors.Add(new Tuple<string, string, Blog>("",
                    $"The word {badWord} is prohibited, please choose another category.", blog));

            ValidationError = true;
        }

        //Let the user know if over 5 bad words are in their categories.
        else if (categoryBadWords.Count is > 0 and > 5)
        {
            BlogModelStateErrors.Add(new Tuple<string, string, Blog>("",
                "You have more than 5 prohibited words in your categories, please revise.", blog));
            ValidationError = true;
        }
    }

    private void CheckImageForValidity(Blog blog)
    {
        if (blog.Image == null)
        {
            //...nothing to check
        }

        //if there is a file, run basic check to make sure its an image, the right size, and decent.
        else if ((blog.Image != null && blog.Image.IsImage() == false)
                 || blog.Image!.ImageSizeIsCorrect() == false
                 || blog.Image!.IsDecent() == false)
        {
            BlogModelStateErrors.Add(new Tuple<string, string, Blog>("", "Please check your image.", blog));
            BlogModelStateErrors.Add(new Tuple<string, string, Blog>("Image",
                "Please check your image and try again.",
                blog));
            ValidationError = true;
        }
    }

    private void CheckSlugForDuplicate(Blog blog, [Optional] Blog? blogToUpdate)
    {
        //if this is not null it is a blog edit
        if (blogToUpdate != null)
        {
            var newSlug = _slugService.UrlFriendly(blog.Name);
            //Let the user know if blog name/slug is used.
            blogToUpdate.Slug = _slugService.UrlFriendly(blogToUpdate.Slug!);
            var slugIsUnique = !_context.Blogs.Any(b => b.Slug!.ToLower() == newSlug.ToLower());

            if (newSlug != blogToUpdate.Slug && !slugIsUnique)
            {
                BlogModelStateErrors.Add(new Tuple<string, string, Blog>("", "Please check blog name.",
                    blogToUpdate));
                BlogModelStateErrors.Add(new Tuple<string, string, Blog>("Name",
                    "The name you've provided cannot be used as there is already a blog using it.", blogToUpdate));
                ValidationError = true;
            }
        }
    }
}