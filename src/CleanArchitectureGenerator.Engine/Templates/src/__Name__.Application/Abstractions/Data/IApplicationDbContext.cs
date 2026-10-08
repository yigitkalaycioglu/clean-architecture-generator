//#if Sample
using __Name__.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;
//#endif

namespace __Name__.Application.Abstractions.Data;

/// <summary>
/// İşleyicilerin veritabanına eriştiği arayüz; Infrastructure'daki ApplicationDbContext uygular.
/// Yeni bir varlık için buraya bir DbSet ekleyin.
/// </summary>
public interface IApplicationDbContext
{
//#if Sample
    DbSet<TodoItem> TodoItems { get; }

//#endif
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
