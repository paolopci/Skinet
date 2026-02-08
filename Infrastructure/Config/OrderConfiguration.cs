using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Ordini");

        builder.Property(order => order.Id)
            .HasColumnName("OrderId");

        builder.Property(order => order.UserId)
            .HasColumnName("UserId")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(order => order.OrderDate)
            .HasColumnName("DataOrdine")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(order => order.PaymentType)
            .HasColumnName("TipoPagamento")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(order => order.CardNumberMasked)
            .HasColumnName("NumeroCarta")
            .HasMaxLength(20);

        builder.Property(order => order.OrderTotal)
            .HasColumnName("TotaleOrdine")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(order => order.OrderStatus)
            .HasColumnName("StatoOrdine")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.OrderDate);
        builder.HasIndex(order => order.PaymentType);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
