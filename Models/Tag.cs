using System.ComponentModel.DataAnnotations;

namespace Portfolio.Models;

public class Tag
{
    //Keys and FK
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string BlogUserId { get; set; } = default!;

    //Comment Text
    [Required]
    [StringLength(25, ErrorMessage = "The {0} must be at least {2} and no more than {1}.", MinimumLength = 2)]
    public string Text { get; set; } = default!;

    //Navigation Properties
    public virtual Post Post { get; set; } = default!;
    public virtual BlogUser? BlogUser { get; set; } = default!;
}