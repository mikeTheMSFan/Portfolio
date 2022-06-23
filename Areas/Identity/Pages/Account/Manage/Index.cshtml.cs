// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portfolio.Data;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Services.Interfaces;

namespace Portfolio.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IRemoteImageService _remoteImageService;
    private readonly SignInManager<BlogUser> _signInManager;
    private readonly UserManager<BlogUser> _userManager;

    public IndexModel(
        UserManager<BlogUser> userManager,
        SignInManager<BlogUser> signInManager,
        ApplicationDbContext context,
        IRemoteImageService remoteImageService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _remoteImageService = remoteImageService;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; }

    private async Task LoadAsync(BlogUser user)
    {
        var userName = await _userManager.GetUserNameAsync(user);
        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

        Username = userName;

        if (User.IsInRole("Administrator"))
        {
            var facebookUrl = string.Empty;
            if (!string.IsNullOrEmpty(user.FacebookUrl)) facebookUrl = user.FacebookUrl;

            var instagramUrl = string.Empty;
            if (!string.IsNullOrEmpty(user.InstagramUrl)) instagramUrl = user.InstagramUrl;

            var pinterestUrl = string.Empty;
            if (!string.IsNullOrEmpty(user.PinterestUrl)) pinterestUrl = user.PinterestUrl;

            var twitterUrl = string.Empty;
            if (!string.IsNullOrEmpty(user.TwitterUrl)) twitterUrl = user.TwitterUrl;

            var youTubeUrl = string.Empty;
            if (!string.IsNullOrEmpty(user.YouTubeUrl)) youTubeUrl = user.YouTubeUrl;

            var authorDescriptionUrl = string.Empty;
            if (!string.IsNullOrEmpty(user.AuthorDescription)) authorDescriptionUrl = user.AuthorDescription;

            Input = new InputModel
            {
                FacebookUrl = facebookUrl,
                InstagramUrl = instagramUrl,
                PinterestUrl = pinterestUrl,
                TwitterUrl = twitterUrl,
                YouTubeUrl = youTubeUrl,
                AuthorDescription = authorDescriptionUrl,
                PhoneNumber = phoneNumber
            };
        }
        else
        {
            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

        if (Input.Image == null)
        {
            //Nothing to process
        }

        else if (Input.Image != null && Input.Image.IsImage()
                                     && Input.Image.IsDecent())
        {
            if (user.FileName != null)
            {
                var pattern = @"([a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12})";
                var match = Regex.Match(user.FileName, pattern);

                if (match.Success)
                {
                    var fileExtension =
                        user.FileName.Substring(user.FileName.Length >= 4 ? user.FileName.Length - 4 : 0);
                    var fullFileName = $"{match.Value}{fileExtension}";
                    var fileName = _remoteImageService.UploadPostImage(Input.Image, fullFileName);
                    user.FileName = fileName;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var fileName = _remoteImageService.UploadProfileImage(Input.Image);
                    user.FileName = fileName;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            var userFileName = _remoteImageService.UploadProfileImage(Input.Image);
            user.FileName = userFileName;
            _context.Update(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            StatusMessage = "Please check your image.";
            return RedirectToPage();
        }

        if (User.IsInRole("Administrator"))
        {
            if (!string.IsNullOrEmpty(Input.FacebookUrl)) user.FacebookUrl = Input.FacebookUrl;

            if (!string.IsNullOrEmpty(Input.InstagramUrl)) user.InstagramUrl = Input.InstagramUrl;

            if (!string.IsNullOrEmpty(Input.PinterestUrl)) user.PinterestUrl = Input.PinterestUrl;

            if (!string.IsNullOrEmpty(Input.TwitterUrl)) user.TwitterUrl = Input.TwitterUrl;

            if (!string.IsNullOrEmpty(Input.YouTubeUrl)) user.YouTubeUrl = Input.YouTubeUrl;

            if (!string.IsNullOrEmpty(Input.AuthorDescription)) user.AuthorDescription = Input.AuthorDescription;
        }

        await _userManager.UpdateAsync(user);

        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (Input.PhoneNumber != phoneNumber)
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to set phone number.";
                return RedirectToPage();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Avatar")] public IFormFile Image { get; set; }

        //Social media URLS
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
            MinimumLength = 2)]
        public string FacebookUrl { get; set; } = "";

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
            MinimumLength = 2)]
        public string InstagramUrl { get; set; } = "";

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
            MinimumLength = 2)]
        public string PinterestUrl { get; set; } = "";

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
            MinimumLength = 2)]
        public string YouTubeUrl { get; set; } = "";

        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
            MinimumLength = 2)]
        public string TwitterUrl { get; set; } = "";

        [Display(Name = "Author Description")]
        [StringLength(500, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
            MinimumLength = 20)]
        public string AuthorDescription { get; set; }
    }
}