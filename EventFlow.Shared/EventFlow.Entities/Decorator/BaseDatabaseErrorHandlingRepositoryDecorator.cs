using Microsoft.Extensions.Logging;
using Npgsql;

namespace EventFlow.Entities.Decorator;
public abstract class BaseDatabaseErrorHandlingRepositoryDecorator<TRepository>
    where TRepository : class
{
    protected readonly TRepository _inner;
    protected readonly ILogger _logger;

    protected BaseDatabaseErrorHandlingRepositoryDecorator(
        TRepository inner,
        ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    protected async Task<T> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string methodName,
        string context)
    {
        try
        {
            return await operation();
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, 
                "Ошибка PostgreSQL в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, 
                "Ошибка подключения к PostgreSQL в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Неожиданная ошибка в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
    }

    protected async Task ExecuteWithErrorHandlingAsync(
        Func<Task> operation,
        string methodName,
        string context)
    {
        try
        {
            await operation();
        }
        catch (PostgresException ex)
        {
            _logger.LogError(ex, 
                "Ошибка PostgreSQL в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, 
                "Ошибка подключения к PostgreSQL в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Неожиданная ошибка в методе {MethodName}. {Context}", 
                methodName, context);
            throw;
        }
    }
}