using Blog.Domain.Entities;
using Blog.Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blog.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        // Apply migrations
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Migration failed. Continuing with seeding (assuming database exists).");
        }

        // 1. Seed Roles
        await SeedRolesAsync(roleManager);

        // 2. Seed Users (Admin + Authors)
        var users = await SeedUsersAsync(userManager);

        // 3. Seed Tags
        var tags = await SeedTagsAsync(context);

        // 4. Seed Posts
        await SeedPostsAsync(context, users, tags);
        logger.LogInformation("Seeding check completed.");
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "Admin", "Moderator", "Author", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<List<ApplicationUser>> SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var users = new List<ApplicationUser>();

        // Admin
        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@devof.net",
            DisplayName = "Administrator",
            EmailConfirmed = true,
            IsActive = true,
            Bio = "System Administrator and Site Manager.",
            CreatedAt = DateTime.UtcNow.AddYears(-1)
        };

        if (await userManager.FindByEmailAsync(admin.Email) == null)
        {
            await userManager.CreateAsync(admin, "Admin@123");
            await userManager.AddToRolesAsync(admin, new[] { "Admin", "Author" });
            users.Add(admin);
        }
        var foundAdmin = await userManager.FindByEmailAsync(admin.Email);
        if (foundAdmin != null)
        {
            // Ensure password is correct
            var token = await userManager.GeneratePasswordResetTokenAsync(foundAdmin);
            await userManager.ResetPasswordAsync(foundAdmin, token, "Admin@123");
            users.Add(foundAdmin);
        }


        // Custom Authors
        var authors = new[]
        {
            new { Name = "Avnish", Email = "avnish@devof.net",
                Bio = "Full-stack developer passionate about .NET and Cloud Architecture." },
            new { Name = "Vikas", Email = "vikas@devof.net", Bio = "Frontend wizard and UI/UX enthusiast. Loves React and clean design." },
            new { Name = "Pooja", Email = "pooja@devof.net", Bio = "Data scientist / Backend engineer. Python & C# expert." }
        };

        foreach (var authorData in authors)
        {
            var user = new ApplicationUser
            {
                UserName = authorData.Name.ToLower(),
                Email = authorData.Email,
                DisplayName = authorData.Name,
                EmailConfirmed = true,
                IsActive = true,
                Bio = authorData.Bio,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            };

            if (await userManager.FindByEmailAsync(user.Email) == null)
            {
                await userManager.CreateAsync(user, "User@123"); // Simple password for demo
                await userManager.AddToRoleAsync(user, "Author");
                users.Add(user);
            }
            else
            {
                var foundUser = await userManager.FindByEmailAsync(user.Email);
                if (foundUser != null)
                {
                    users.Add(foundUser);
                }
            }
        }

        return users;
    }

    private static async Task<List<Tag>> SeedTagsAsync(ApplicationDbContext context)
    {
        if (context.Tags.Any()) return await context.Tags.ToListAsync();

        var tags = new[]
        {
            new Tag { Name = "C#", Slug = "csharp", Description = "The C# programming language", Color = "#68217A" },
            new Tag { Name = ".NET", Slug = "dotnet", Description = "The .NET ecosystem", Color = "#512BD4" },
            new Tag { Name = "ASP.NET Core", Slug = "aspnetcore", Description = "ASP.NET Core web framework", Color = "#1E90FF" },
            new Tag { Name = "JavaScript", Slug = "javascript", Description = "JavaScript programming language", Color = "#F7DF1E" },
            new Tag { Name = "TypeScript", Slug = "typescript", Description = "TypeScript programming language", Color = "#3178C6" },
            new Tag { Name = "DevOps", Slug = "devops", Description = "DevOps practices and tools", Color = "#FF6B6B" },
            new Tag { Name = "Database", Slug = "database", Description = "Database technologies", Color = "#336791" },
            new Tag { Name = "Cloud", Slug = "cloud", Description = "Cloud computing", Color = "#FF9900" },
            new Tag { Name = "Tutorial", Slug = "tutorial", Description = "Learning tutorials", Color = "#28A745" },
            new Tag { Name = "Career", Slug = "career", Description = "Career advice and growth", Color = "#6C757D" }
        };

        context.Tags.AddRange(tags);
        await context.SaveChangesAsync();
        return tags.ToList();
    }

    private static async Task SeedPostsAsync(ApplicationDbContext context, List<ApplicationUser> users, List<Tag> tags)
    {
        var random = new Random();
        var posts = new List<Post>();

        var sampleTitles = new[]
        {
            ("Getting Started with .NET 8", "Comprehensive guide to the new features in .NET 8."),
            ("Why Clean Architecture Matters", "Structuring your applications for maintainability."),
            ("Mastering Async/Await in C#", "Deep dive into asynchronous programming patterns."),
            ("Responsive Design with CSS Grid", "Building modern layouts without frameworks."),
            ("Intro to Docker for Developers", "Containerizing your .NET applications."),
            ("Entity Framework Core Performance Tips", "Optimizing your database queries."),
            ("Building REST APIs with minimal APIs", "Fast and lightweight API development."),
            ("Blazor vs React: A Comparison", "Choosing the right frontend technology."),
            ("Deploying to Azure App Service", "Step-by-step deployment guide."),
            ("Understanding Dependency Injection", "The backbone of modern ASP.NET Core apps."),
            ("JavaScript ES6+ Features You Should Know", "Modernizing your JS code."),
            ("The Future of AI in Development", "How LLMs are changing the workflow."),
            ("Secure Authentication with Identity", "Implementing login flows correctly."),
            ("Unit Testing with xUnit and Moq", "Ensuring code quality with tests."),
            ("Microservices vs Monoliths", "Architectural trade-offs explained.")
        };

        foreach (var (title, desc) in sampleTitles)
        {
            // Skip if post with same title already exists
            if (await context.Posts.AnyAsync(p => p.Title == title)) continue;

            var author = users[random.Next(users.Count)];
            var createdDate = DateTime.UtcNow.AddDays(-random.Next(1, 100));

            var post = new Post
            {
                Title = title,
                Slug = title.ToLower().Replace(" ", "-").Replace(":", "").Replace("/", "-"),
                Content = GenerateMarkdownContent(title, desc),
                Excerpt = desc,
                AuthorId = author.Id,
                Status = PostStatus.Published,
                CreatedAt = createdDate,
                PublishedAt = createdDate,
                ReadingTimeMinutes = random.Next(3, 15),
                ViewCount = random.Next(10, 500)
            };

            // Add Tags
            int tagCount = random.Next(1, 4);
            for (int i = 0; i < tagCount; i++)
            {
                var tag = tags[random.Next(tags.Count)];
                if (!post.PostTags.Any(pt => pt.Tag!.Id == tag.Id))
                {
                    post.PostTags.Add(new PostTag { Tag = tag });
                }
            }

            // Add Comments
            int commentCount = random.Next(0, 8);
            for (int i = 0; i < commentCount; i++)
            {
                var commenter = users[random.Next(users.Count)];
                post.Comments.Add(new Comment
                {
                    Content = $"Great post! Thanks for sharing, {author.DisplayName}.",
                    AuthorId = commenter.Id,
                    CreatedAt = createdDate.AddHours(random.Next(1, 48))
                });
            }

            // Add Likes
            int likeCount = random.Next(0, 15);
            for (int i = 0; i < likeCount; i++)
            {
                var liker = users[random.Next(users.Count)];
                if (!post.Likes.Any(l => l.UserId == liker.Id))
                {
                    post.Likes.Add(new Like { UserId = liker.Id });
                }
            }

            posts.Add(post);
        }

        context.Posts.AddRange(posts);
        await context.SaveChangesAsync();
    }

    private static string GenerateMarkdownContent(string title, string description)
    {
        return $@"
# {title}

{description}

## Introduction

In this article, we'll explore **{title}** and why it's important for modern development.

## Key Concepts

1. **Simplicity**: Easy to understand.
2. **Performance**: Optimized for speed.
3. **Scale**: Built to grow.

### Code Example

Here is a snippet to demonstrate:

```csharp
public void HelloWorld()
{{
    Console.WriteLine(""Hello from Devof.NET!"");
}}
```

## Conclusion

I hope you found this guide helpful! Feel free to leave a comment below.

> ""Innovation distinguishes between a leader and a follower.""

Thanks for reading!
";
    }
}
