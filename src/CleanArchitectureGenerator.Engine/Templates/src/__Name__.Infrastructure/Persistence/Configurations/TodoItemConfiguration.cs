//#if Sample
using __Name__.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace __Name__.Infrastructure.Persistence.Configurations;

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("TodoItems");
        builder.HasKey(todoItem => todoItem.Id);
        builder.Property(todoItem => todoItem.Id).ValueGeneratedNever();

        builder.Property(todoItem => todoItem.OwnerId).HasMaxLength(TodoItem.OwnerIdMaxLength).IsRequired();
        builder.Property(todoItem => todoItem.Title).HasMaxLength(TodoItem.TitleMaxLength).IsRequired();
        builder.Property(todoItem => todoItem.Note).HasMaxLength(TodoItem.NoteMaxLength);
        builder.Property(todoItem => todoItem.Priority).HasConversion<string>().HasMaxLength(16);

        // Kullanıcının listesi sahibe göre, en yeniden eskiye okunur.
        builder.HasIndex(todoItem => new { todoItem.OwnerId, todoItem.CreatedAt });
    }
}
//#endif
