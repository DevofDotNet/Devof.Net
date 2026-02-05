using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blog.Infrastructure.Data.Configurations;

public class CookieConsentConfiguration : IEntityTypeConfiguration<CookieConsent>
{
    public void Configure(EntityTypeBuilder<CookieConsent> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ConsentType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(c => c.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        // Optional relationship to User (null for anonymous)
        builder.HasOne(c => c.User)
            .WithMany(u => u.CookieConsents)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.IpAddress);
    }
}
