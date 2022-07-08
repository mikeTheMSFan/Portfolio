using System.ComponentModel.DataAnnotations;
using Portfolio.Enums;

namespace Portfolio.Models.Content;

public class Comment
{
    //Key and FKs
    [Key] public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string BlogUserId { get; set; } = default!;
    public string? ModeratorId { get; set; } = default!;

    //Comment Body
    [Required]
    [StringLength(500, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters.",
        MinimumLength = 2)]
    [Display(Name = "Comment")]
    public string Body { get; set; } = default!;

    //Date Records
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime? Moderated { get; set; }
    public DateTime? Deleted { get; set; }

    //Moderation Body
    [StringLength(500, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters.",
        MinimumLength = 2)]
    [Display(Name = "Moderated Comment")]
    public string? ModeratedBody { get; set; }

    //Why comment was moderated
    public ModerationType ModerationType { get; set; }

    //Navigation Properties
    public virtual Post? Post { get; set; }

    [Display(Name = "Author")] public virtual BlogUser? BlogUser { get; set; }

    [Display(Name = "Moderator")] public virtual BlogUser? Moderator { get; set; }
}