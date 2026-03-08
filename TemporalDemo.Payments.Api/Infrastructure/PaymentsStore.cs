using Microsoft.EntityFrameworkCore;

namespace TemporalDemo.Payments.Api.Infrastructure;

public sealed class PaymentsStore(IDbContextFactory<PaymentsDbContext> dbContextFactory)
{
    public async Task<IReadOnlyCollection<PaymentRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Payments
            .AsNoTracking()
            .OrderBy(x => x.UpdatedAtUtc)
            .Select(x => new PaymentRecord(x.OrderId, x.Amount, x.Status, x.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PaymentRecord?> GetAsync(string orderId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .Select(x => new PaymentRecord(x.OrderId, x.Amount, x.Status, x.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentRecord> ChargeAsync(
        string orderId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var payment = await dbContext.Payments.SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
        payment ??= new PaymentEntity { OrderId = orderId };

        payment.Amount = amount;
        payment.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (amount > 5_000m)
        {
            payment.Status = "declined";
            if (dbContext.Entry(payment).State == EntityState.Detached)
            {
                dbContext.Payments.Add(payment);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Payment declined for order '{orderId}' because amount exceeds limit.");
        }

        payment.Status = "approved";
        if (dbContext.Entry(payment).State == EntityState.Detached)
        {
            dbContext.Payments.Add(payment);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PaymentRecord(payment.OrderId, payment.Amount, payment.Status, payment.UpdatedAtUtc);
    }
}

public sealed record PaymentRecord(string OrderId, decimal Amount, string Status, DateTimeOffset UpdatedAtUtc);