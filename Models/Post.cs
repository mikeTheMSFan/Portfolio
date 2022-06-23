using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Portfolio.Enums;

namespace Portfolio.Models;

public class Post
{
    //Key and FKs
    [JsonIgnore] [Key] public Guid Id { get; set; }

    [Display(Name = "Blog Name")]
    [JsonIgnore]
    public Guid BlogId { get; set; }

    [JsonIgnore] public Guid CategoryId { get; set; } = default!;

    [JsonIgnore] public string BlogUserId { get; set; } = default!;

    //Post title and abstract
    [Required]
    [StringLength(75, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 2)]
    public string Title { get; set; } = default!;

    [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 2)]
    public string? Abstract { get; set; } = default!;
    //Post content

    [Required] public string Content { get; set; } = default!;

    //Date created and updated
    [DataType(DataType.Date)]
    [Display(Name = "Created Date")]
    public DateTime Created { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Updated Date")]
    public DateTime? Updated { get; set; }

    //Define the status of post
    [JsonIgnore] public ReadyStatus ReadyStatus { get; set; }

    //Slug to find post
    public string? Slug { get; set; } = default!;

    //Post image info
    public string? FileName { get; set; }

    [JsonIgnore] [NotMapped] public IFormFile? Image { get; set; }


    //Navigation Properties
    [Display(Name = "Blog")] [JsonIgnore] public virtual Blog? Blog { get; set; } = default!;

    [Display(Name = "Author")] public virtual BlogUser? BlogUser { get; set; } = default!;

    public virtual Category? Category { get; set; } = default!;
    public virtual ICollection<Tag>? Tags { get; set; } = new HashSet<Tag>();
    public virtual ICollection<Comment>? Comments { get; set; } = new HashSet<Comment>();
}