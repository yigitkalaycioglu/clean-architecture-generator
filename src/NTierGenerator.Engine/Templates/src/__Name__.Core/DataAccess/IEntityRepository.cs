using System.Linq.Expressions;
using __Name__.Core.Entities;

namespace __Name__.Core.DataAccess;

/// <summary>
/// Tüm veri erişim sınıflarının ortak CRUD sözleşmesi. Her varlığın Dal arayüzü bundan türer
/// ve yalnızca o varlığa özel sorguları (join, rapor vb.) ekler.
/// </summary>
public interface IEntityRepository<TEntity>
    where TEntity : class, IEntity, new()
{
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
