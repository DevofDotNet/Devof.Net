using Blog.Application.Services;
using Blog.Application.Validators;
using Blog.Domain.Entities;
using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data;
using Blog.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production with email service
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// OAuth (Google) - only add if configured
// OAuth
var authBuilder = builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrEmpty(githubClientId) && !string.IsNullOrEmpty(githubClientSecret))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
    });
}

// Repositories & Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application Services
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IEngagementService, EngagementService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();

// Image Service
var uploadPath = Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads");
var siteUrl = builder.Configuration["AppSettings:SiteUrl"] ?? "https://localhost:5001";
builder.Services.AddSingleton<IImageService>(new LocalImageService(uploadPath, siteUrl));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePostValidator>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
    options.Conventions.AuthorizeFolder("/Settings");
    options.Conventions.AuthorizePage("/Post/Create");
    options.Conventions.AuthorizePage("/Post/Edit");
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin", "Moderator"));
    options.AddPolicy("AuthorPolicy", policy => policy.RequireRole("Admin", "Author"));
});

// Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Create database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Apply migrations
        await context.Database.MigrateAsync();
        
        // Seed roles
        var roles = new[] { "Admin", "Moderator", "Author", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
        
        // Seed admin user
        var adminEmail = "admin@devof.net";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = adminEmail,
                DisplayName = "Administrator",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(adminUser, new[] { "Admin", "Author" });
            }
        }
        
        // Seed sample tags
        if (!context.Tags.Any())
        {
            var tags = new[]
            {
                new Blog.Domain.Entities.Tag { Name = "C#", Slug = "csharp", Description = "The C# programming language", Color = "#68217A" },
                new Blog.Domain.Entities.Tag { Name = ".NET", Slug = "dotnet", Description = "The .NET ecosystem", Color = "#512BD4" },
                new Blog.Domain.Entities.Tag { Name = "ASP.NET Core", Slug = "aspnetcore", Description = "ASP.NET Core web framework", Color = "#1E90FF" },
                new Blog.Domain.Entities.Tag { Name = "JavaScript", Slug = "javascript", Description = "JavaScript programming language", Color = "#F7DF1E" },
                new Blog.Domain.Entities.Tag { Name = "TypeScript", Slug = "typescript", Description = "TypeScript programming language", Color = "#3178C6" },
                new Blog.Domain.Entities.Tag { Name = "DevOps", Slug = "devops", Description = "DevOps practices and tools", Color = "#FF6B6B" },
                new Blog.Domain.Entities.Tag { Name = "Database", Slug = "database", Description = "Database technologies", Color = "#336791" },
                new Blog.Domain.Entities.Tag { Name = "Cloud", Slug = "cloud", Description = "Cloud computing", Color = "#FF9900" },
                new Blog.Domain.Entities.Tag { Name = "Tutorial", Slug = "tutorial", Description = "Learning tutorials", Color = "#28A745" },
                new Blog.Domain.Entities.Tag { Name = "Career", Slug = "career", Description = "Career advice and growth", Color = "#6C757D" }
            };
            context.Tags.AddRange(tags);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
