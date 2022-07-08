using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Portfolio.Models.Content;

namespace Portfolio.Models.Filters;

public class Category
{
    [Key] public Guid Id { get; set; }

    [JsonIgnore] public Guid BlogId { get; set; }

    [Required]
    [StringLength(35, ErrorMessage = "The {0} must be between {2} and {1} characters long.", MinimumLength = 7)]
    public string Name { get; set; } = default!;

    //Navigation Properties
    [JsonIgnore] public virtual Blog Blog { get; set; } = default!;

    [JsonIgnore] public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();
}