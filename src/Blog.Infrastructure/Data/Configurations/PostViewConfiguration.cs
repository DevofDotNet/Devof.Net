using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class PostViewConfiguration : IEntityTypeConfiguration<PostView>
{
    public void Configure(EntityTypeBuilder<PostView> builder)
    {
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.IpAddress)
            .IsRequired()
            .HasMaxLength(45); // IPv6 max length

        builder.Property(pv => pv.UserAgent)
            .HasMaxLength(500);

        // One Post has many PostViews
        builder.HasOne(pv => pv.Post)
            .WithMany(p => p.PostViews)
            .HasForeignKey(pv => pv.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // One User (optional) has many PostViews
        builder.HasOne(pv => pv.Viewer)
            .WithMany(u => u.PostViews)
            .HasForeignKey(pv => pv.ViewerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(pv => pv.PostId);
        builder.HasIndex(pv => pv.ViewerId);
        builder.HasIndex(pv => pv.ViewedAt);
    }
}
