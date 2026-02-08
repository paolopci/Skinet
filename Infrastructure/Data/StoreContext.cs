using Core.Entities;
using Infrastructure.Config;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContext(DbContextOptions<StoreContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<DeliveryMethod> DeliveryMethods { get; set; }
    public DbSet<PaymentOrder> PaymentOrders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().OwnsOne(user => user.Address, address =>
        {
            address.WithOwner();
            address.Property(a => a.FirstName).HasMaxLength(100);
            address.Property(a => a.LastName).HasMaxLength(100);
            address.Property(a => a.AddressLine1).HasMaxLength(200);
            address.Property(a => a.AddressLine2).HasMaxLength(200);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.PostalCode).HasMaxLength(20);
            address.Property(a => a.CountryCode).HasMaxLength(2);
            address.Property(a => a.Region).HasMaxLength(100);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(512).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(512);
            entity.Property(token => token.CreatedByIp).HasMaxLength(64);
            entity.Property(token => token.UserAgent).HasMaxLength(256);
            entity.HasOne(token => token.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductConfiguration).Assembly);
    }
}
