using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.Models;

namespace Portfolio.Data;

public class ApplicationDbContext : IdentityDbContext<BlogUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Blog> Blogs { get; set; } = default!;
    public DbSet<Post> Posts { get; set; } = default!;
    public DbSet<Comment> Comments { get; set; } = default!;
    public DbSet<Tag> Tags { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<Project> Projects { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Blog>().Property(x => x.Id).HasAnnotation("MySql:ValueGeneratedOnAdd", true);
        builder.Entity<Post>().Property(x => x.Id).HasAnnotation("MySql:ValueGeneratedOnAdd", true);
        builder.Entity<Comment>().Property(x => x.Id).HasAnnotation("MySql:ValueGeneratedOnAdd", true);
        builder.Entity<Tag>().Property(x => x.Id).HasAnnotation("MySql:ValueGeneratedOnAdd", true);
        builder.Entity<Category>().Property(x => x.Id).HasAnnotation("MySql:ValueGeneratedOnAdd", true);
        builder.Entity<Project>().Property(x => x.Id).HasAnnotation("MySql:ValueGeneratedOnAdd", true);
    }
}