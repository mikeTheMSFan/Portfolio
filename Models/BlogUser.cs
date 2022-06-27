using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace Portfolio.Models;

[MetadataType(typeof(ApplicationUserMetaData))]
public sealed class BlogUser : IdentityUser
{
    //First name
    [StringLength(20, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    //Last name
    [StringLength(20, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = "";

    [Display(Name = "Author Description")]
    [StringLength(500, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 20)]
    public string? AuthorDescription { get; set; }

    //Profile image properties
    [JsonIgnore] public string? FileName { get; set; }

    // [System.Text.Json.Serialization.JsonIgnore]
    // public string? ContentType { get; set; }

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

    //First and last name
    [NotMapped] [JsonIgnore] public string FullName => $"{FirstName} {LastName}";

    [NotMapped] public string? base64ProfileImage { get; set; } = default!;

    //Navigation properties
    [JsonIgnore] public ICollection<Blog> Blogs { get; set; } = new HashSet<Blog>();

    [JsonIgnore] public ICollection<Post> Posts { get; set; } = new HashSet<Post>();


    // ... thank you https://gist.github.com/theuntitled/7c70fff994993d7644f12d5bb0dc205f

    #region overrides

    [JsonIgnore] public override string? Email { get; set; }

    [JsonIgnore] public override string? UserName { get; set; }

    [JsonIgnore] public override string? NormalizedUserName { get; set; }

    [JsonIgnore] public override string? NormalizedEmail { get; set; }

    [JsonIgnore] public override string? ConcurrencyStamp { get; set; }

    [JsonIgnore] public override DateTimeOffset? LockoutEnd { get; set; }

    [JsonIgnore] public override string? Id { get; set; }

    [JsonIgnore] public override bool EmailConfirmed { get; set; }

    [JsonIgnore] public override bool TwoFactorEnabled { get; set; }

    [JsonIgnore] public override string? PhoneNumber { get; set; }

    [JsonIgnore] public override bool PhoneNumberConfirmed { get; set; }

    [JsonIgnore] public override string? PasswordHash { get; set; }

    [JsonIgnore] public override string? SecurityStamp { get; set; }

    [JsonIgnore] public override bool LockoutEnabled { get; set; }

    [JsonIgnore] public override int AccessFailedCount { get; set; }

    #endregion
}