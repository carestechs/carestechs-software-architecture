using Common.Lib.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Common.Providers.Data;

public sealed class EfUnitOfWork<TContext>(TContext context) : IUnitOfWork
    where TContext : DbContext
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
