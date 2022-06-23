using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Portfolio.Data;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Services;
using Portfolio.Services.Interfaces;
using Portfolio.ViewModels;

var builder = WebApplication.CreateBuilder(args);
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build()
    .Decrypt("CipherKey", "CipherText:");

// Add services to the container.
var connectionString = configuration.GetConnectionString("Production");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//Add built-in identity
builder.Services.AddIdentity<BlogUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddTokenProvider<DataProtectorTokenProvider<BlogUser>>(TokenOptions.DefaultProvider)
    .AddDefaultTokenProviders();

//Add Microsoft Authentication (External login)
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

//Add Google Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = configuration.GetSection("GoogleAccount").GetSection("client_id").Value;
        options.ClientSecret = configuration.GetSection("GoogleAccount").GetSection("client_secret").Value;
        options.ClaimActions.MapJsonKey("GivenName", "given_name");
        options.ClaimActions.MapJsonKey("Surname", "family_name");
        options.ClaimActions.MapJsonKey("picture", "picture");
    });

//Setup Microsoft as External Authenticator
builder.Services
    .AddOptions()
    .PostConfigureAll<OpenIdConnectOptions>(o =>
    {
        o.ClaimActions.MapJsonKey("GivenName", "given_name");
        o.ClaimActions.MapJsonKey("Surname", "family_name");
        o.SignInScheme = IdentityConstants.ExternalScheme;
        o.SaveTokens = true;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHttpContextAccessor();

//Register the slug service
builder.Services.AddScoped<ISlugService, BasicSlugService>();

//Register the data service
builder.Services.AddScoped<DataService>();

//Register a preconfigured instance of the MailSettings class
var mailSettings = configuration.GetSection("MailSettings");
builder.Services.Configure<MailSettingsViewModel>(mailSettings);
builder.Services.AddScoped<IBlogEmailSender, EmailService>();

//Register Remote Image Service
builder.Services.AddSingleton<IRemoteImageService, BasicRemoteImageService>();

//Register Civility Service
builder.Services.AddSingleton<ICivility, BasicCivilityService>();

//Register Validation Service
builder.Services.AddScoped<IValidate, ValidateService>();

//Register Avatar Service
builder.Services.AddScoped<INoAvatarService, BasicAvatarService>();

//Register External Login Service
builder.Services.AddScoped<IExternalLogin, BasicExternalLoginService>();

//Register Swagger Implementation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Mike's Post Citing API",
        Description = "An extremely simple api to allow users to easily use my posts on their site.",
        Contact = new OpenApiContact
        {
            Name = "Contact",
            Url = new Uri("https://localhost:7061/Home/Contact")
        }
    });
    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

//Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();

app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlogAPI"); });

app.UseHttpsRedirection();

//must be before UseStaticFiles
app.UseDefaultFiles();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    "article-details",
    "article/{slug}",
    new { Controller = "Posts", Action = "Details" });

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    "delete-category",
    "blogs/DeleteCategory/{name}",
    new { Controller = "Blogs", Action = "DeleteCategory" });


app.MapRazorPages();

//Pull out registered data service
var dataService = app.Services
    .CreateScope()
    .ServiceProvider
    .GetRequiredService<DataService>();

await dataService.ManageDataAsync();

app.Run();