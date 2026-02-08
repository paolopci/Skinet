using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
{
    public void Configure(EntityTypeBuilder<OrderDetail> builder)
    {
        builder.ToTable("DettaglioOrdine");

        builder.Property(detail => detail.Id)
            .HasColumnName("DettaglioId");

        builder.Property(detail => detail.OrderId)
            .HasColumnName("OrderId")
            .IsRequired();

        builder.Property(detail => detail.ProductId)
            .HasColumnName("ProdottoId")
            .IsRequired();

        builder.Property(detail => detail.Quantity)
            .HasColumnName("Quantita")
            .IsRequired();

        builder.Property(detail => detail.UnitPrice)
            .HasColumnName("PrezzoUnitario")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(detail => detail.Order)
            .WithMany(order => order.Details)
            .HasForeignKey(detail => detail.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(detail => detail.Product)
            .WithMany()
            .HasForeignKey(detail => detail.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
