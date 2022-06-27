﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Identity.Web;
using Portfolio.Extensions;
using Portfolio.Helpers;
using Portfolio.Models;
using Portfolio.Services.Interfaces;
using SixLabors.ImageSharp;

namespace Portfolio.Areas.Identity.Pages.Account;

[AllowAnonymous]
[AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
public class ExternalLoginModel : PageModel
{
    private readonly IBlogEmailSender _emailSender;
    private readonly IUserEmailStore<BlogUser> _emailStore;
    private readonly ILogger<ExternalLoginModel> _logger;
    private readonly IRemoteImageService _remoteImageService;
    private readonly SignInManager<BlogUser> _signInManager;
    private readonly UserManager<BlogUser> _userManager;
    private readonly IUserStore<BlogUser> _userStore;

    public ExternalLoginModel(
        SignInManager<BlogUser> signInManager,
        UserManager<BlogUser> userManager,
        IUserStore<BlogUser> userStore,
        ILogger<ExternalLoginModel> logger,
        IBlogEmailSender emailSender,
        IRemoteImageService remoteImageService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = GetEmailStore();
        _logger = logger;
        _emailSender = emailSender;
        _remoteImageService = remoteImageService;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; }

    [BindProperty] public ClaimsInputModel Claims { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string ProviderDisplayName { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string ReturnUrl { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        return RedirectToPage("./Login");
    }

    public IActionResult OnPost(string provider, string returnUrl = null)
    {
        // Request a redirect to the external login provider.
        var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }
        
        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name,
                info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut) return RedirectToPage("./Lockout");

        // If the user does not have an account, then ask the user to create an account.
        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;
        if (info.Principal.HasClaim(c => c.Type.Any()))
        {
            var base64ProfilePicture = string.Empty;
            if (info.ProviderDisplayName.ToLower() == "microsoft")
            {
                try
                {
                    var token = info.AuthenticationTokens.FirstOrDefault(t => t.Name == "id_token")!.Value;

                    var singleUserGraphClient = await MicrosoftGraph.GetMicrosoftGraphSingleUserClient(token);
                    
                    //use Graph client to get stream of photo.
                    var stream = await singleUserGraphClient.Me.Photos["120x120"]
                        .Content
                        .Request()
                        .GetAsync();

                    if (stream == null) base64ProfilePicture = string.Empty;
                    
                    //create image from stream
                    var image = Image.Load(stream, out var format);

                    //return base64 image.
                    base64ProfilePicture = image.ToBase64String(format);
                }
                catch (Exception)
                {
                    ErrorMessage = "Error loading external login information.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
            }
            else if (info.ProviderDisplayName.ToLower() == "google")
            {
                var profileImageUrl = info.Principal.FindFirstValue("picture");
                var client = new HttpClient();
            
                var response = await client.GetAsync(profileImageUrl);
                var stream = await response.Content.ReadAsStreamAsync();
            
                var image = Image.Load(stream, out var format);
                base64ProfilePicture = image.ToBase64String(format);
            }
            else
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
        
            Claims = new ClaimsInputModel
            {
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname),
                Base64ProfilePicture = base64ProfilePicture
            };
        
            Input = new InputModel
            {
                Email = info.Principal.FindFirstValue(ClaimTypes.Email)
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        // Get the information about the user from the external login provider
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid)
        {
            var user = CreateUser();
            
            //Populate user
            user.FirstName = Claims.FirstName;
            user.LastName = Claims.LastName;
            user.base64ProfileImage = Claims.Base64ProfilePicture;

            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    if (Claims.ProfilePicture.IsImage())
                    {
                        if (user.FileName != null)
                        {
                            var match = Regex.Match(user.FileName,
                                @"[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?");
                            user.FileName = match.Success
                                ? _remoteImageService.UploadPostImage(Claims.ProfilePicture, match.Value)
                                : _remoteImageService.UploadPostImage(Claims.ProfilePicture);
                        }

                        user.FileName = _remoteImageService.UploadProfileImage(Claims.ProfilePicture);
                    }

                    result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                    
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            null,
                            new { area = "Identity", userId, code },
                            Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                            return RedirectToPage("./RegisterConfirmation", new { Input.Email });

                        await _signInManager.SignInAsync(user, false, info.LoginProvider);
                        return LocalRedirect(returnUrl); 
                    }
                }
            }

            ReturnUrl = returnUrl;
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        }

        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        return Page();
    }

    private BlogUser CreateUser()
    {
        try
        {
            return Activator.CreateInstance<BlogUser>();
        }
        catch
        {
            throw new InvalidOperationException($"Can't create an instance of '{nameof(BlogUser)}'. " +
                                                $"Ensure that '{nameof(BlogUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                                                "override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
        }
    }

    private IUserEmailStore<BlogUser> GetEmailStore()
    {
        if (!_userManager.SupportsUserEmail)
            throw new NotSupportedException("The default UI requires a user store with email support.");
        return (IUserEmailStore<BlogUser>)_userStore;
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
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ClaimsInputModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Base64ProfilePicture { get; set; }

        [JsonIgnore] public IFormFile ProfilePicture { get; set; }
    }
}