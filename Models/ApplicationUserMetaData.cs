using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Portfolio.Models;

public class ApplicationUserMetaData
{
    [JsonIgnore] public ICollection<IdentityUserClaim<string>> Claims { get; } = default!;
    [JsonIgnore] public ICollection<IdentityUserLogin<string>> Logins { get; } = default!;
    [JsonIgnore] public ICollection<IdentityUserRole<string>> Roles { get; } = default!;
}