using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Content)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>();

        // One User has many Notifications
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional relationship to Post
        builder.HasOne(n => n.RelatedPost)
            .WithMany()
            .HasForeignKey(n => n.RelatedPostId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional relationship to Comment
        builder.HasOne(n => n.RelatedComment)
            .WithMany()
            .HasForeignKey(n => n.RelatedCommentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional relationship to another User (for follow notifications)
        builder.HasOne(n => n.RelatedUser)
            .WithMany()
            .HasForeignKey(n => n.RelatedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.CreatedAt);
    }
}
