using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Portfolio.Models.Filters;

namespace Portfolio.Models.Content;

public class Blog
{
    //Key and FK
    [Key] public Guid Id { get; set; }

    public string AuthorId { get; set; } = default!;

    //Name of blog and desc.
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters.", MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(500, ErrorMessage = "The {0} must be at least {2} and at most {1} characters.", MinimumLength = 2)]
    public string Description { get; set; } = default!;

    //Blog Created or updated
    [DataType(DataType.Date)]
    [Display(Name = "Created Date")]
    public DateTime Created { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Updated Date")]
    public DateTime? Updated { get; set; }

    //Blog image file and type
    [Display(Name = "Blog Image")] public string? FileName { get; set; } = default!;


    public string? Slug { get; set; } = default!;

    //Holds image from user
    [NotMapped] public IFormFile? Image { get; set; }

    //Navigation Properties
    public virtual BlogUser? Author { get; set; } = default!;
    public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();
    public virtual ICollection<Category>? Categories { get; set; } = new HashSet<Category>();
}