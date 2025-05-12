using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PdfSmith.DataAccessLayer.Entities;

namespace PdfSmith.DataAccessLayer.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())");

        builder.Property(e => e.ApiKey).HasMaxLength(2048).IsUnicode(false);
        builder.Property(e => e.UserName).HasMaxLength(255);
    }
}
