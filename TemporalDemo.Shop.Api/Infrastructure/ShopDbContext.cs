using Microsoft.EntityFrameworkCore;

namespace TemporalDemo.Shop.Api.Infrastructure;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public const string Schema = "shop";

    public DbSet<ShopOrderEntity> Orders => Set<ShopOrderEntity>();
    public DbSet<ShopProductEntity> Products => Set<ShopProductEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ShopProductEntity>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ShopOrderEntity>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.OrderId);
            entity.Property(x => x.OrderId).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(64);
            entity.Property(x => x.FailureReason).HasMaxLength(1024);
        });
    }
}

public sealed class ShopProductEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public sealed class ShopOrderEntity
{
    public string OrderId { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}