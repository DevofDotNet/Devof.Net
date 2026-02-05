using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class TagFollowConfiguration : IEntityTypeConfiguration<TagFollow>
{
    public void Configure(EntityTypeBuilder<TagFollow> builder)
    {
        builder.HasKey(tf => tf.Id);

        // One User has many TagFollows
        builder.HasOne(tf => tf.User)
            .WithMany(u => u.TagFollows)
            .HasForeignKey(tf => tf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Tag has many TagFollows
        builder.HasOne(tf => tf.Tag)
            .WithMany(t => t.TagFollows)
            .HasForeignKey(tf => tf.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate follows
        builder.HasIndex(tf => new { tf.UserId, tf.TagId })
            .IsUnique();

        builder.HasIndex(tf => tf.TagId);
    }
}
