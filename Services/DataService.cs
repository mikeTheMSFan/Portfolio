using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Enums;
using Portfolio.Models;

namespace Portfolio.Services;

public class DataService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<BlogUser> _userManager;

    public DataService(ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<BlogUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task ManageDataAsync()
    {
        //create the db from the migrations
        await _context.Database.MigrateAsync();

        //seed a few roles into the system.
        await SeedRolesAsync();

        //seed a few users into the system.
        await SeedUsersAsync();
    }

    private async Task SeedRolesAsync()
    {
        //If there are already roles in the system, do nothing.
        if (_context.Roles.Any()) return;

        //Otherwise, create a few roles.
        foreach (var role in Enum.GetNames(typeof(BlogRole)))
            //Use role manager to create roles.
            await _roleManager.CreateAsync(new IdentityRole(role));
    }

    private async Task SeedUsersAsync()
    {
        if (_context.Users.Any()) return;

        //Create new instance of BlogUser
        var adminUser = new BlogUser
        {
            Email = "bigmike2238@gmx.com",
            UserName = "bigmike2238@gmx.com",
            FirstName = "Michael",
            LastName = "Robinson",
            PhoneNumber = "(800) 555-1212",
            EmailConfirmed = true
        };

        //Use UserManager to create new user that is defined by the adminUser.
        await _userManager.CreateAsync(adminUser, "ctDzC6KszXUjkoY%feX&C");

        //Add user to the administrator role
        await _userManager.AddToRoleAsync(adminUser, BlogRole.Administrator.ToString());

        //Create new instance of BlogUser for moderator
        var modUser = new BlogUser
        {
            Email = "mike.maurice.robinson@gmail.com",
            UserName = "mike.maurice.robinson@gmail.com",
            FirstName = "Michael",
            LastName = "Robinson",
            PhoneNumber = "(800) 555-1212",
            EmailConfirmed = true
        };

        //Use UserManager to create new user that is defined by the modUser
        await _userManager.CreateAsync(modUser, "3Hnu!CwY*6&v87gxs@776");

        //Add user to moderator role.
        await _userManager.AddToRoleAsync(modUser, BlogRole.Moderator.ToString());
    }
}