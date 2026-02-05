using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName).HasMaxLength(100);
        builder.Property(u => u.Bio).HasMaxLength(500);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.WebsiteUrl).HasMaxLength(200);
        builder.Property(u => u.GitHubUrl).HasMaxLength(200);
        builder.Property(u => u.TwitterUrl).HasMaxLength(200);
        builder.Property(u => u.LinkedInUrl).HasMaxLength(200);
        builder.Property(u => u.Location).HasMaxLength(100);
        builder.Property(u => u.BanReason).HasMaxLength(500);

        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.CreatedAt);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(250);
        builder.Property(p => p.Content).IsRequired().HasColumnType("LONGTEXT");
        builder.Property(p => p.RenderedContent).HasColumnType("LONGTEXT");
        builder.Property(p => p.Excerpt).HasMaxLength(500);
        builder.Property(p => p.CoverImageUrl).HasMaxLength(500);
        builder.Property(p => p.MetaTitle).HasMaxLength(200);
        builder.Property(p => p.MetaDescription).HasMaxLength(500);
        builder.Property(p => p.MetaKeywords).HasMaxLength(300);
        builder.Property(p => p.CreatedBy).HasMaxLength(450);
        builder.Property(p => p.UpdatedBy).HasMaxLength(450);

        // Indexes
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.PublishedAt);
        builder.HasIndex(p => p.TrendingScore);
        builder.HasIndex(p => p.CreatedAt);

        // Full-text search index for MySQL (only on Title, Content is LONGTEXT and can't be indexed)
        builder.HasIndex(p => p.Title)
               .HasDatabaseName("IX_Posts_TitleSearch");

        // Relationships
        builder.HasOne(p => p.Author)
               .WithMany(u => u.Posts)
               .HasForeignKey(p => p.AuthorId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(60);
        builder.Property(t => t.Description).HasMaxLength(300);
        builder.Property(t => t.IconUrl).HasMaxLength(500);
        builder.Property(t => t.Color).HasMaxLength(7); // #FFFFFF

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.Name);
    }
}

public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.HasKey(pt => new { pt.PostId, pt.TagId });

        builder.HasOne(pt => pt.Post)
               .WithMany(p => p.PostTags)
               .HasForeignKey(pt => pt.PostId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
               .WithMany(t => t.PostTags)
               .HasForeignKey(pt => pt.TagId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired().HasMaxLength(5000);
        builder.Property(c => c.RenderedContent).HasMaxLength(6000);

        builder.HasIndex(c => c.PostId);
        builder.HasIndex(c => c.AuthorId);
        builder.HasIndex(c => c.ParentCommentId);
        builder.HasIndex(c => c.CreatedAt);

        // Self-referencing relationship for nested comments
        builder.HasOne(c => c.ParentComment)
               .WithMany(c => c.Replies)
               .HasForeignKey(c => c.ParentCommentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Post)
               .WithMany(p => p.Comments)
               .HasForeignKey(c => c.PostId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
               .WithMany(u => u.Comments)
               .HasForeignKey(c => c.AuthorId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.HasKey(l => l.Id);

        // Unique constraint: one like per user per post
        builder.HasIndex(l => new { l.UserId, l.PostId }).IsUnique();
        builder.HasIndex(l => l.PostId);

        builder.HasOne(l => l.User)
               .WithMany(u => u.Likes)
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Post)
               .WithMany(p => p.Likes)
               .HasForeignKey(l => l.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.HasKey(b => b.Id);

        // Unique constraint: one bookmark per user per post
        builder.HasIndex(b => new { b.UserId, b.PostId }).IsUnique();
        builder.HasIndex(b => b.UserId);

        builder.HasOne(b => b.User)
               .WithMany(u => u.Bookmarks)
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Post)
               .WithMany(p => p.Bookmarks)
               .HasForeignKey(b => b.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.HasKey(f => f.Id);

        // Unique constraint: one follow relationship per pair
        builder.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();
        builder.HasIndex(f => f.FollowerId);
        builder.HasIndex(f => f.FollowingId);

        builder.HasOne(f => f.Follower)
               .WithMany(u => u.Following)
               .HasForeignKey(f => f.FollowerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Following)
               .WithMany(u => u.Followers)
               .HasForeignKey(f => f.FollowingId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Details).HasMaxLength(1000);
        builder.Property(r => r.ModeratorNotes).HasMaxLength(1000);
        builder.Property(r => r.ResolvedById).HasMaxLength(450);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.Type);
        builder.HasIndex(r => r.CreatedAt);

        builder.HasOne(r => r.Reporter)
               .WithMany(u => u.ReportsSubmitted)
               .HasForeignKey(r => r.ReporterId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReportedUser)
               .WithMany(u => u.ReportsReceived)
               .HasForeignKey(r => r.ReportedUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedPost)
               .WithMany(p => p.Reports)
               .HasForeignKey(r => r.ReportedPostId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ReportedComment)
               .WithMany(c => c.Reports)
               .HasForeignKey(r => r.ReportedCommentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
