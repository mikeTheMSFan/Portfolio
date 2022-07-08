using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Portfolio.Models.Content;

public class Project
{
    [Key] [JsonIgnore] public Guid Id { get; set; }

    [Required]
    [StringLength(30, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 5)]
    public string Type { get; set; } = default!;

    [Required]
    [StringLength(60, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 5)]
    public string Title { get; set; } = default!;

    [Required]
    [StringLength(200, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.",
        MinimumLength = 15)]
    public string Description { get; set; } = default!;

    [DataType(DataType.Date)]
    [Display(Name = "Create Date")]
    public DateTime? Created { get; set; }

    [Required] public string ProjectUrl { get; set; } = default!;

    [JsonIgnore] public string? ContentUrl { get; set; }

    [JsonIgnore] [NotMapped] public IFormFile? Image { get; set; }

    [NotMapped] public string? Base64ProjectPicture { get; set; }
}