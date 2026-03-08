using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace TemporalDemo.Shop.Api.Infrastructure;

public sealed class ShopDatabaseInitializer(
    IDbContextFactory<ShopDbContext> dbContextFactory,
    ILogger<ShopDatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await SchemaObjectsExistAsync(dbContext, "products", cancellationToken))
        {
            var script = dbContext.Database.GenerateCreateScript();
            await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
            logger.LogInformation("Created shop schema objects");
        }

        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Products.AddRange(
            new ShopProductEntity
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Laptop",
                Price = 1200m,
                Stock = 5,
            },
            new ShopProductEntity
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Headphones",
                Price = 250m,
                Stock = 12,
            },
            new ShopProductEntity
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Mouse",
                Price = 80m,
                Stock = 25,
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded shop products");
    }

    private static async Task<bool> SchemaObjectsExistAsync(
        ShopDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                                  SELECT EXISTS (
                                      SELECT 1
                                      FROM information_schema.tables
                                      WHERE table_schema = @schema
                                        AND table_name = @table
                                  );
                                  """;
            AddParameter(command, "schema", ShopDbContext.Schema);
            AddParameter(command, "table", tableName);

            return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}