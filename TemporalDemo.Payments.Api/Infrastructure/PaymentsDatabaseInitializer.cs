using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace TemporalDemo.Payments.Api.Infrastructure;

public sealed class PaymentsDatabaseInitializer(
    IDbContextFactory<PaymentsDbContext> dbContextFactory,
    ILogger<PaymentsDatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await SchemaObjectsExistAsync(dbContext, "payments", cancellationToken))
        {
            return;
        }

        var script = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
        logger.LogInformation("Created payments schema objects");
    }

    private static async Task<bool> SchemaObjectsExistAsync(
        PaymentsDbContext dbContext,
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
            AddParameter(command, "schema", PaymentsDbContext.Schema);
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