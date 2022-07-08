using System.Reflection;
using System.Security.Claims;
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi.Models;
using Portfolio.Areas.Identity.Pages.Account;
using Portfolio.Data;
using Portfolio.Enums;
using Portfolio.Extensions;
using Portfolio.Models;
using Portfolio.Models.Settings;
using Portfolio.Services;
using Portfolio.Services.Interfaces;
using Portfolio.ViewModels;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build()
    .Decrypt("CipherKey", "CipherText:");

builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

// Add services to the container.
var connectionString = configuration.GetConnectionString("Production");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//Add built-in identity
builder.Services.AddIdentity<BlogUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddTokenProvider<DataProtectorTokenProvider<BlogUser>>(TokenOptions.DefaultProvider)
    .AddDefaultTokenProviders();

//Add Microsoft Authentication (External login)
string[] initialScopes = configuration.GetValue<string>("AppSettings:DownstreamApi:Scopes")?.Split(' ')!;
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApp(configuration.GetSection("AppSettings").GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
    .AddMicrosoftGraph(configuration.GetSection("AppSettings").GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = "https://login.microsoftonline.com/common/v2.0/";
    options.ClientId = configuration.GetSection("AppSettings").GetSection("AzureAd").GetSection("ClientId").Value;
    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
    options.CallbackPath = configuration.GetSection("AppSettings").GetSection("AzureAd").GetSection("CallBackPath").Value;
    options.RequireHttpsMetadata = false;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("email");
    options.Scope.Add("offline_access");
    options.Scope.Add("profile");
    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.ClientSecret = configuration.GetSection("AppSettings").GetSection("AzureAd").GetSection("ClientSecret").Value;
    options.SignInScheme = IdentityConstants.ExternalScheme;
});

//Change the display name of scheme
builder.Services.PostConfigure<AuthenticationOptions>(options =>
{
    foreach (var scheme in options.Schemes)
    {
        if (scheme.Name == OpenIdConnectDefaults.AuthenticationScheme)
        {
            scheme.DisplayName = "Microsoft";
        }
    }
});

//Add Google Authentication (Disabled till verified)
    // builder.Services.AddAuthentication()
    // .AddGoogle(options =>
    // {
    //     options.ClientId = configuration.GetSection("GoogleAccount").GetSection("client_id").Value;
    //     options.ClientSecret = configuration.GetSection("GoogleAccount").GetSection("client_secret").Value;
    //     options.ClaimActions.MapJsonKey("GivenName", "given_name");
    //     options.ClaimActions.MapJsonKey("Surname", "family_name");
    //     options.ClaimActions.MapJsonKey("picture", "picture");
    // });

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

//Register External Login Services
builder.Services.AddScoped<IExternalLoginService, MicrosoftExternalLogin>();
builder.Services.AddScoped<IExternalLoginService, GoogleExternalLogin>();

// builder.Services.AddScoped<ExternalLoginResolver>(serviceProvider => serviceTypeName =>
// {
//     switch (serviceTypeName)
//     {
//         case ServiceType.MicrosoftExternalLogin:
//             return serviceProvider.GetService<MicrosoftExternalLogin>();
//         case ServiceType.GoogleExternalLogin:
//             return serviceProvider.GetService<GoogleExternalLogin>();
//         default:
//             return null;
//     }
// });

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
            Url = new Uri("https://www.mikemrobinsondev.com/Home/Contact")
        }
    });
    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

//Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

//Set recaptcha configuration
builder.Services.AddReCaptcha(configuration.GetSection("ReCaptcha"));

var app = builder.Build();

app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == 404)
    {
        context.Request.Path = "/EndUserErrors/NotFoundError";
        await next();
    }
});

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