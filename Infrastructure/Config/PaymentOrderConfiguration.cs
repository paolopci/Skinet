using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.Property(order => order.CartId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(order => order.PaymentIntentId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(order => order.UserId)
            .HasMaxLength(450);

        builder.Property(order => order.Currency)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(order => order.FailureMessage)
            .HasMaxLength(1000);

        builder.HasIndex(order => order.PaymentIntentId)
            .IsUnique();

        builder.HasIndex(order => order.CartId);
        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.OrderId);

        builder.HasOne(order => order.Order)
            .WithMany()
            .HasForeignKey(order => order.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
