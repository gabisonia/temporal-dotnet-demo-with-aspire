using Microsoft.EntityFrameworkCore;

namespace TemporalDemo.Payments.Api.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public const string Schema = "payments";

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(x => x.OrderId);
            entity.Property(x => x.OrderId).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(64);
        });
    }
}

public sealed class PaymentEntity
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; }
}