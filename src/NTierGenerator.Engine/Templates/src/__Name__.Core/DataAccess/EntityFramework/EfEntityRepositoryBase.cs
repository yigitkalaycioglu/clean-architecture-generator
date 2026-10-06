using System.Linq.Expressions;
using __Name__.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace __Name__.Core.DataAccess.EntityFramework;

/// <summary>
/// <see cref="IEntityRepository{TEntity}"/> sözleşmesinin Entity Framework Core uygulaması.
/// Somut Dal sınıfları bundan türeyerek CRUD işlemlerini hazır alır. DbContext DI ile gelir
/// ve istek (scope) boyunca paylaşılır.
/// </summary>
public abstract class EfEntityRepositoryBase<TEntity, TContext>(TContext context) : IEntityRepository<TEntity>
    where TEntity : class, IEntity, new()
    where TContext : DbContext
{
    protected TContext Context { get; } = context;

    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        return Context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(filter, cancellationToken);
    }

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var query = Context.Set<TEntity>().AsNoTracking();
        return (filter is null ? query : query.Where(filter)).ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var set = Context.Set<TEntity>();
        return filter is null ? set.AnyAsync(cancellationToken) : set.AnyAsync(filter, cancellationToken);
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var set = Context.Set<TEntity>();
        return filter is null ? set.CountAsync(cancellationToken) : set.CountAsync(filter, cancellationToken);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Set<TEntity>().Add(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Set<TEntity>().Update(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Set<TEntity>().Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
