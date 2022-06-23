using System.ComponentModel.DataAnnotations;

namespace Portfolio.ViewModels;

public class ContactViewModel
{
    [Required]
    [StringLength(65, ErrorMessage = "The {0} must be at least {2} and at most {1}. ", MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [Required]
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = default!;

    [Phone]
    [DataType(DataType.PhoneNumber)]
    [StringLength(13, ErrorMessage = "The {0} must be at least {2} and and most {1}.", MinimumLength = 10)]
    public string PhoneNumber { get; set; } = default!;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1}.", MinimumLength = 5)]
    public string Subject { get; set; } = default!;

    [Required] public string Body { get; set; } = default!;
}